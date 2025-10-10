package utils

import (
	"fmt"
	"math/rand"
	"time"
)

func GenerateOrderNumber() string {
	rand.Seed(time.Now().UnixNano())

	now := time.Now()
	datePart := now.Format("20060102")
	randomPart := rand.Intn(9999) + 1000

	return fmt.Sprintf("ORD-%s-%d", datePart, randomPart)
}