package services

import (
	"context"
	"fmt"
	"log"
	"order-service/database"
	"order-service/models"
	"order-service/utils"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type OrderService struct {
	collection      *mongo.Collection
	messagePublisher *MessagePublisher
}

func NewOrderService() *OrderService {
	collection := database.DB.GetCollection("orders")
	messagePublisher := NewMessagePublisher()

	return &OrderService{
		collection:      collection,
		messagePublisher: messagePublisher,
	}
}

func (s *OrderService) CreateOrder(ctx context.Context, req *models.CreateOrderRequest) (*models.Order, error) {
	totalAmount := 0.0
	for i, item := range req.Items {
		req.Items[i].TotalPrice = item.UnitPrice * float64(item.Quantity)
		totalAmount += req.Items[i].TotalPrice
	}

	order := &models.Order{
		ID:              primitive.NewObjectID(),
		OrderNumber:     utils.GenerateOrderNumber(),
		UserID:          req.UserID,
		UserEmail:       req.UserEmail,
		Items:           req.Items,
		TotalAmount:     totalAmount,
		Status:          models.StatusPending,
		ShippingAddress: req.ShippingAddress,
		CreatedAt:       time.Now(),
		UpdatedAt:       time.Now(),
	}

	_, err := s.collection.InsertOne(ctx, order)
	if err != nil {
		return nil, fmt.Errorf("failed to create order: %w", err)
	}

	if err := s.messagePublisher.PublishOrderCreated(order); err != nil {
		log.Printf("Failed to publish order created event: %v", err)
	}

	return order, nil
}

func (s *OrderService) GetOrderByID(ctx context.Context, id string) (*models.Order, error) {
	objectID, err := primitive.ObjectIDFromHex(id)
	if err != nil {
		return nil, fmt.Errorf("invalid order ID: %w", err)
	}

	var order models.Order
	err = s.collection.FindOne(ctx, bson.M{"_id": objectID}).Decode(&order)
	if err != nil {
		if err == mongo.ErrNoDocuments {
			return nil, fmt.Errorf("order not found")
		}
		return nil, fmt.Errorf("failed to get order: %w", err)
	}

	return &order, nil
}

func (s *OrderService) GetOrdersByUserID(ctx context.Context, userID int64) ([]models.Order, error) {
	filter := bson.M{"user_id": userID}
	opts := options.Find().SetSort(bson.M{"created_at": -1})

	cursor, err := s.collection.Find(ctx, filter, opts)
	if err != nil {
		return nil, fmt.Errorf("failed to get orders: %w", err)
	}
	defer cursor.Close(ctx)

	var orders []models.Order
	if err = cursor.All(ctx, &orders); err != nil {
		return nil, fmt.Errorf("failed to decode orders: %w", err)
	}

	return orders, nil
}

func (s *OrderService) GetAllOrders(ctx context.Context) ([]models.Order, error) {
	opts := options.Find().SetSort(bson.M{"created_at": -1})

	cursor, err := s.collection.Find(ctx, bson.M{}, opts)
	if err != nil {
		return nil, fmt.Errorf("failed to get orders: %w", err)
	}
	defer cursor.Close(ctx)

	var orders []models.Order
	if err = cursor.All(ctx, &orders); err != nil {
		return nil, fmt.Errorf("failed to decode orders: %w", err)
	}

	return orders, nil
}

func (s *OrderService) UpdateOrderStatus(ctx context.Context, id string, status models.OrderStatus) (*models.Order, error) {
	objectID, err := primitive.ObjectIDFromHex(id)
	if err != nil {
		return nil, fmt.Errorf("invalid order ID: %w", err)
	}

	update := bson.M{
		"$set": bson.M{
			"status":     status,
			"updated_at": time.Now(),
		},
	}

	var order models.Order
	err = s.collection.FindOneAndUpdate(
		ctx,
		bson.M{"_id": objectID},
		update,
		options.FindOneAndUpdate().SetReturnDocument(options.After),
	).Decode(&order)

	if err != nil {
		if err == mongo.ErrNoDocuments {
			return nil, fmt.Errorf("order not found")
		}
		return nil, fmt.Errorf("failed to update order: %w", err)
	}

	if err := s.messagePublisher.PublishOrderStatusUpdated(&order); err != nil {
		log.Printf("Failed to publish order status updated event: %v", err)
	}

	return &order, nil
}

func (s *OrderService) GetOrderStats(ctx context.Context) (map[string]interface{}, error) {
	totalOrdersPipeline := []bson.M{
		{"$group": bson.M{"_id": nil, "count": bson.M{"$sum": 1}}},
	}

	totalRevenuesPipeline := []bson.M{
		{"$group": bson.M{"_id": nil, "totalRevenue": bson.M{"$sum": "$total_amount"}}},
	}

	statusStatsPipeline := []bson.M{
		{"$group": bson.M{"_id": "$status", "count": bson.M{"$sum": 1}}},
	}

	totalOrdersCursor, _ := s.collection.Aggregate(ctx, totalOrdersPipeline)
	totalRevenuesCursor, _ := s.collection.Aggregate(ctx, totalRevenuesPipeline)
	statusStatsCursor, _ := s.collection.Aggregate(ctx, statusStatsPipeline)

	var totalOrdersResult []bson.M
	var totalRevenuesResult []bson.M
	var statusStatsResult []bson.M

	totalOrdersCursor.All(ctx, &totalOrdersResult)
	totalRevenuesCursor.All(ctx, &totalRevenuesResult)
	statusStatsCursor.All(ctx, &statusStatsResult)

	totalOrders := int64(0)
	if len(totalOrdersResult) > 0 {
		if count, ok := totalOrdersResult[0]["count"].(int32); ok {
			totalOrders = int64(count)
		}
	}

	totalRevenue := 0.0
	if len(totalRevenuesResult) > 0 {
		if revenue, ok := totalRevenuesResult[0]["totalRevenue"].(float64); ok {
			totalRevenue = revenue
		}
	}

	stats := map[string]interface{}{
		"totalOrders":  totalOrders,
		"totalRevenue": totalRevenue,
		"statusStats":  statusStatsResult,
		"timestamp":    time.Now().Unix(),
	}

	return stats, nil
}