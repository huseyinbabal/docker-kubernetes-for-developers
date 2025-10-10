package services

import (
	"encoding/json"
	"fmt"
	"log"
	"order-service/models"
	"os"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

type MessagePublisher struct {
	conn    *amqp.Connection
	channel *amqp.Channel
}

const ORDER_EXCHANGE = "order.exchange"

func NewMessagePublisher() *MessagePublisher {
	rabbitMQURL := fmt.Sprintf("amqp://%s:%s@%s:%s/",
		getEnv("RABBITMQ_USER", "guest"),
		getEnv("RABBITMQ_PASSWORD", "guest"),
		getEnv("RABBITMQ_HOST", "localhost"),
		getEnv("RABBITMQ_PORT", "5672"),
	)

	conn, err := amqp.Dial(rabbitMQURL)
	if err != nil {
		log.Printf("Failed to connect to RabbitMQ: %v", err)
		return &MessagePublisher{} // Return empty publisher if connection fails
	}

	channel, err := conn.Channel()
	if err != nil {
		log.Printf("Failed to open RabbitMQ channel: %v", err)
		conn.Close()
		return &MessagePublisher{}
	}

	err = channel.ExchangeDeclare(
		ORDER_EXCHANGE,
		"topic",
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		log.Printf("Failed to declare exchange: %v", err)
		channel.Close()
		conn.Close()
		return &MessagePublisher{}
	}

	return &MessagePublisher{
		conn:    conn,
		channel: channel,
	}
}

func (mp *MessagePublisher) PublishOrderCreated(order *models.Order) error {
	if mp.channel == nil {
		return fmt.Errorf("RabbitMQ channel not available")
	}

	message := map[string]interface{}{
		"eventType":    "ORDER_CREATED",
		"orderId":      order.ID.Hex(),
		"orderNumber":  order.OrderNumber,
		"userId":       order.UserID,
		"userEmail":    order.UserEmail,
		"totalAmount":  order.TotalAmount,
		"status":       order.Status,
		"items":        order.Items,
		"timestamp":    time.Now().Unix(),
	}

	return mp.publishMessage("order.created", message)
}

func (mp *MessagePublisher) PublishOrderStatusUpdated(order *models.Order) error {
	if mp.channel == nil {
		return fmt.Errorf("RabbitMQ channel not available")
	}

	message := map[string]interface{}{
		"eventType":   "ORDER_STATUS_UPDATED",
		"orderId":     order.ID.Hex(),
		"orderNumber": order.OrderNumber,
		"userId":      order.UserID,
		"status":      order.Status,
		"timestamp":   time.Now().Unix(),
	}

	return mp.publishMessage("order.status.updated", message)
}

func (mp *MessagePublisher) publishMessage(routingKey string, message map[string]interface{}) error {
	body, err := json.Marshal(message)
	if err != nil {
		return fmt.Errorf("failed to marshal message: %w", err)
	}

	err = mp.channel.Publish(
		ORDER_EXCHANGE,
		routingKey,
		false,
		false,
		amqp.Publishing{
			ContentType:  "application/json",
			Body:         body,
			DeliveryMode: amqp.Persistent,
		},
	)

	if err != nil {
		return fmt.Errorf("failed to publish message: %w", err)
	}

	return nil
}

func (mp *MessagePublisher) Close() {
	if mp.channel != nil {
		mp.channel.Close()
	}
	if mp.conn != nil {
		mp.conn.Close()
	}
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}