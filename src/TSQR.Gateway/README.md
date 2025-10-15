# TSQR.Gateway README.md

# TSQR.Gateway

TSQR.Gateway is an API gateway designed for a microservice architecture. It provides a unified entry point for various microservices, handling requests, security, and performance enhancements.

## Features

- **Request Handling**: Efficiently routes incoming requests to the appropriate microservices.
- **Security**: Implements authentication and rate limiting to protect microservices.
- **Performance Enhancements**: Aggregates responses from multiple services to minimize latency.

## Project Structure

- **TSQR.Gateway.WebApi**: Contains the Web API implementation, including controllers and middleware.
- **TSQR.Gateway.Application**: Contains application logic and services for routing and aggregating responses.
- **TSQR.Gateway.Domain**: Defines the domain models used throughout the application.
- **TSQR.Gateway.Infrastructure**: Handles security and infrastructure-related concerns.
- **TSQR.Gateway.Tests**: Contains unit tests for the API gateway components.

## Setup Instructions

1. Clone the repository:
   ```
   git clone <repository-url>
   ```

2. Navigate to the project directory:
   ```
   cd TSQR.Gateway
   ```

3. Restore the dependencies:
   ```
   dotnet restore
   ```

4. Run the application:
   ```
   dotnet run --project TSQR.Gateway.WebApi
   ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.