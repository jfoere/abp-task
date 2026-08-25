# Technical Assignment

## Task Context

You work for a company that develops an application for managing conference-room bookings and rentals. Your task is to create an API for managing rooms and bookings and calculating rental costs.

## Problem Description

The company rents conference rooms to businesses. You need to develop a simple API that allows clients to find available rooms, book them, and calculate the rental cost based on the booking time and selected services.

## Technical Requirements

### API Methods

1. **Create a conference room:**
   - Input: Room name (for example, "Room A"), capacity (for example, 50 people), a list of available services (for example, projector at UAH 500 and Wi-Fi at UAH 300), and the base hourly rental rate (for example, UAH 2,000).
   - Output: Confirmation that the room was created successfully, including its unique ID.

2. **Update conference-room information:**
   - Input: Room ID and updated data (for example, changing the rental rate to UAH 2,500 or adding the "Sound" service at UAH 700).
   - Output: Confirmation that the room was updated successfully.

3. **Delete a conference room:**
   - Input: Room ID.
   - Output: Confirmation that the room was deleted.

4. **Search for available rooms:**
   - Input: Date, time range, and required capacity (for example, September 1, 2024, from 10:00 to 14:00, for 50 people).
   - Output: A list of available rooms.

5. **Book a room:**
   - Input: Room ID, booking date and time, duration, and selected services.
   - Output: Booking confirmation with the calculated total rental cost.

## Initial Data

### Rooms

- Room A: capacity of 50 people; base rate of UAH 2,000 per hour.
- Room B: capacity of 100 people; base rate of UAH 3,500 per hour.
- Room C: capacity of 30 people; base rate of UAH 1,500 per hour.

### Services

- Projector: UAH 500.
- Wi-Fi: UAH 300.
- Sound: UAH 700.

## Rental Cost Calculation

- The rental cost depends on the booking time:
  - Standard hours (09:00–18:00): the room's base rate.
  - Evening hours (18:00–23:00): a 20% discount on the room rental rate.
  - Morning hours (06:00–09:00): a 10% discount.
  - Peak hours (12:00–14:00): a 15% surcharge.

## Additional Requirements

1. **Clean code and scalability:** Follow the practices described in Robert C. Martin's *Clean Code* when implementing the solution. The project will be expanded in the future, so it is important that it be scalable, secure, and fault-tolerant. Provide an appropriate level of security to prevent issues for clients who use the API.
2. **Reporting and analytics:** Design and add reports that would be useful to the business.

## Considered a Plus

- A complete Git README.
- Code comments.
- API documentation using Swagger.

## Submission Format

- A link to the source-code repository.
- Brief project documentation describing the business requirements and technical decisions.
