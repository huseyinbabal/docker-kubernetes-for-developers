package database

import (
	"context"
	"fmt"
	"log"
	"os"
	"time"

	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type MongoDB struct {
	Client   *mongo.Client
	Database *mongo.Database
}

var DB *MongoDB

func InitMongoDB() {
	host := getEnv("MONGO_HOST", "localhost")
	port := getEnv("MONGO_PORT", "27017")
	dbName := getEnv("MONGO_DB_NAME", "orderdb")
	username := getEnv("MONGO_USERNAME", "")
	password := getEnv("MONGO_PASSWORD", "")

	var uri string
	if username != "" && password != "" {
		uri = fmt.Sprintf("mongodb://%s:%s@%s:%s/%s?authSource=admin", username, password, host, port, dbName)
	} else {
		uri = fmt.Sprintf("mongodb://%s:%s/%s", host, port, dbName)
	}

	clientOptions := options.Client().ApplyURI(uri)

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	client, err := mongo.Connect(ctx, clientOptions)
	if err != nil {
		log.Fatal("Failed to connect to MongoDB:", err)
	}

	err = client.Ping(ctx, nil)
	if err != nil {
		log.Fatal("Failed to ping MongoDB:", err)
	}

	DB = &MongoDB{
		Client:   client,
		Database: client.Database(dbName),
	}

	log.Println("Connected to MongoDB successfully")
}

func (db *MongoDB) GetCollection(collectionName string) *mongo.Collection {
	return db.Database.Collection(collectionName)
}

func (db *MongoDB) Close() {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	if err := db.Client.Disconnect(ctx); err != nil {
		log.Printf("Error disconnecting from MongoDB: %v", err)
	} else {
		log.Println("Disconnected from MongoDB")
	}
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

