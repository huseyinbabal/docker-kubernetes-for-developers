package com.ecommerce.userservice.service;

import com.ecommerce.userservice.model.User;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.Map;

@Service
public class MessagePublisher {

    @Autowired
    private RabbitTemplate rabbitTemplate;

    @Autowired
    private ObjectMapper objectMapper;

    private static final String USER_EXCHANGE = "user.exchange";

    public void publishUserCreated(User user) {
        try {
            Map<String, Object> message = new HashMap<>();
            message.put("eventType", "USER_CREATED");
            message.put("userId", user.getId());
            message.put("username", user.getUsername());
            message.put("email", user.getEmail());
            message.put("firstName", user.getFirstName());
            message.put("lastName", user.getLastName());
            message.put("timestamp", System.currentTimeMillis());

            String jsonMessage = objectMapper.writeValueAsString(message);
            rabbitTemplate.convertAndSend(USER_EXCHANGE, "user.created", jsonMessage);
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Error publishing user created event", e);
        }
    }

    public void publishUserDeactivated(User user) {
        try {
            Map<String, Object> message = new HashMap<>();
            message.put("eventType", "USER_DEACTIVATED");
            message.put("userId", user.getId());
            message.put("username", user.getUsername());
            message.put("timestamp", System.currentTimeMillis());

            String jsonMessage = objectMapper.writeValueAsString(message);
            rabbitTemplate.convertAndSend(USER_EXCHANGE, "user.deactivated", jsonMessage);
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Error publishing user deactivated event", e);
        }
    }
}