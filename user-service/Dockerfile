FROM maven:3-amazoncorretto-21 as builder

WORKDIR /app

COPY . .

RUN mvn clean package

FROM amazoncorretto:21.0.0

COPY --from=builder /app/target/user-service-1.0.0.jar app.jar

EXPOSE 8080

ENTRYPOINT ["java", "-jar", "app.jar"]
