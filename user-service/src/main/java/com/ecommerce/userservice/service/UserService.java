package com.ecommerce.userservice.service;

import com.ecommerce.userservice.dto.UserRegistrationDto;
import com.ecommerce.userservice.dto.UserResponseDto;
import com.ecommerce.userservice.model.User;
import com.ecommerce.userservice.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

@Service
@Transactional
public class UserService {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private PasswordEncoder passwordEncoder;

    @Autowired
    private MessagePublisher messagePublisher;

    public UserResponseDto createUser(UserRegistrationDto registrationDto) {
        if (userRepository.existsByUsername(registrationDto.getUsername())) {
            throw new RuntimeException("Username already exists");
        }

        if (userRepository.existsByEmail(registrationDto.getEmail())) {
            throw new RuntimeException("Email already exists");
        }

        User user = new User();
        user.setUsername(registrationDto.getUsername());
        user.setEmail(registrationDto.getEmail());
        user.setPassword(passwordEncoder.encode(registrationDto.getPassword()));
        user.setFirstName(registrationDto.getFirstName());
        user.setLastName(registrationDto.getLastName());
        user.setPhoneNumber(registrationDto.getPhoneNumber());

        User savedUser = userRepository.save(user);

        messagePublisher.publishUserCreated(savedUser);

        return convertToResponseDto(savedUser);
    }

    @Transactional(readOnly = true)
    public Optional<UserResponseDto> getUserById(Long id) {
        return userRepository.findById(id)
                .map(this::convertToResponseDto);
    }

    @Transactional(readOnly = true)
    public Optional<UserResponseDto> getUserByUsername(String username) {
        return userRepository.findByUsername(username)
                .map(this::convertToResponseDto);
    }

    @Transactional(readOnly = true)
    public List<UserResponseDto> getAllActiveUsers() {
        return userRepository.findAllActiveUsers()
                .stream()
                .map(this::convertToResponseDto)
                .collect(Collectors.toList());
    }

    public UserResponseDto updateUser(Long id, UserRegistrationDto updateDto) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("User not found"));

        if (!user.getUsername().equals(updateDto.getUsername()) &&
            userRepository.existsByUsername(updateDto.getUsername())) {
            throw new RuntimeException("Username already exists");
        }

        if (!user.getEmail().equals(updateDto.getEmail()) &&
            userRepository.existsByEmail(updateDto.getEmail())) {
            throw new RuntimeException("Email already exists");
        }

        user.setUsername(updateDto.getUsername());
        user.setEmail(updateDto.getEmail());
        user.setFirstName(updateDto.getFirstName());
        user.setLastName(updateDto.getLastName());
        user.setPhoneNumber(updateDto.getPhoneNumber());

        if (updateDto.getPassword() != null && !updateDto.getPassword().isEmpty()) {
            user.setPassword(passwordEncoder.encode(updateDto.getPassword()));
        }

        User savedUser = userRepository.save(user);
        return convertToResponseDto(savedUser);
    }

    public void deactivateUser(Long id) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("User not found"));

        user.setIsActive(false);
        userRepository.save(user);

        messagePublisher.publishUserDeactivated(user);
    }

    @Transactional(readOnly = true)
    public long getActiveUserCount() {
        return userRepository.countActiveUsers();
    }

    private UserResponseDto convertToResponseDto(User user) {
        return new UserResponseDto(
                user.getId(),
                user.getUsername(),
                user.getEmail(),
                user.getFirstName(),
                user.getLastName(),
                user.getPhoneNumber(),
                user.getIsActive(),
                user.getCreatedAt(),
                user.getUpdatedAt()
        );
    }
}