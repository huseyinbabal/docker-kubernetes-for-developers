package models

import (
	"time"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type OrderStatus string

const (
	StatusPending   OrderStatus = "PENDING"
	StatusConfirmed OrderStatus = "CONFIRMED"
	StatusProcessing OrderStatus = "PROCESSING"
	StatusShipped   OrderStatus = "SHIPPED"
	StatusDelivered OrderStatus = "DELIVERED"
	StatusCancelled OrderStatus = "CANCELLED"
)

type OrderItem struct {
	ProductID   int64   `json:"productId" bson:"product_id" validate:"required"`
	ProductName string  `json:"productName" bson:"product_name" validate:"required"`
	Quantity    int     `json:"quantity" bson:"quantity" validate:"required,min=1"`
	UnitPrice   float64 `json:"unitPrice" bson:"unit_price" validate:"required,min=0"`
	TotalPrice  float64 `json:"totalPrice" bson:"total_price"`
}

type Order struct {
	ID          primitive.ObjectID `json:"id" bson:"_id,omitempty"`
	OrderNumber string             `json:"orderNumber" bson:"order_number"`
	UserID      int64              `json:"userId" bson:"user_id" validate:"required"`
	UserEmail   string             `json:"userEmail" bson:"user_email" validate:"required,email"`
	Items       []OrderItem        `json:"items" bson:"items" validate:"required,dive"`
	TotalAmount float64            `json:"totalAmount" bson:"total_amount"`
	Status      OrderStatus        `json:"status" bson:"status"`
	CreatedAt   time.Time          `json:"createdAt" bson:"created_at"`
	UpdatedAt   time.Time          `json:"updatedAt" bson:"updated_at"`

	ShippingAddress Address `json:"shippingAddress" bson:"shipping_address" validate:"required"`
}

type Address struct {
	Street     string `json:"street" bson:"street" validate:"required"`
	City       string `json:"city" bson:"city" validate:"required"`
	State      string `json:"state" bson:"state" validate:"required"`
	PostalCode string `json:"postalCode" bson:"postal_code" validate:"required"`
	Country    string `json:"country" bson:"country" validate:"required"`
}

type CreateOrderRequest struct {
	UserID          int64       `json:"userId" validate:"required"`
	UserEmail       string      `json:"userEmail" validate:"required,email"`
	Items           []OrderItem `json:"items" validate:"required,dive"`
	ShippingAddress Address     `json:"shippingAddress" validate:"required"`
}

type UpdateOrderStatusRequest struct {
	Status OrderStatus `json:"status" validate:"required"`
}