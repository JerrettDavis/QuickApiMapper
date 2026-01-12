#!/usr/bin/env python3
"""
Publishes test messages to RabbitMQ for QuickApiMapper integration testing.

Usage:
    python publish-test-message.py --type customer
    python publish-test-message.py --type order --integration OrderIntegration

Requirements:
    pip install pika
"""

import argparse
import json
import random
import uuid
from datetime import datetime
import pika

# Test message templates
CUSTOMER_MESSAGE = {
    "customerId": f"CUST-{random.randint(1000, 9999)}",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phoneNumber": "+1-555-0123",
    "address": {
        "street": "123 Main St",
        "city": "Springfield",
        "state": "IL",
        "zipCode": "62701"
    }
}

ORDER_MESSAGE = {
    "orderId": f"ORD-{random.randint(1000, 9999)}",
    "customerId": "CUST-1234",
    "orderDate": datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%S"),
    "totalAmount": round(random.uniform(100, 10000) / 100, 2),
    "items": [
        {
            "productId": "PROD-001",
            "quantity": 2,
            "unitPrice": 29.99
        },
        {
            "productId": "PROD-002",
            "quantity": 1,
            "unitPrice": 49.99
        }
    ]
}

def publish_message(
    message_type: str,
    integration_name: str = None,
    exchange: str = "quickapi.exchange",
    routing_key: str = None,
    hostname: str = "localhost",
    port: int = 5672,
    username: str = "guest",
    password: str = "guest"
):
    """Publish a test message to RabbitMQ."""

    # Set defaults based on message type
    if integration_name is None:
        integration_name = f"{message_type.capitalize()}Integration"

    if routing_key is None:
        routing_key = f"{message_type}.created"

    # Get message payload
    message = CUSTOMER_MESSAGE if message_type == "customer" else ORDER_MESSAGE

    print(f"\n{'='*60}")
    print(f"Publishing {message_type} message to RabbitMQ")
    print(f"{'='*60}")
    print(f"Exchange: {exchange}")
    print(f"Routing Key: {routing_key}")
    print(f"Integration: {integration_name}")
    print(f"\nMessage payload:")
    print(json.dumps(message, indent=2))
    print(f"{'='*60}\n")

    try:
        # Create connection
        credentials = pika.PlainCredentials(username, password)
        parameters = pika.ConnectionParameters(
            host=hostname,
            port=port,
            credentials=credentials
        )

        connection = pika.BlockingConnection(parameters)
        channel = connection.channel()

        # Declare exchange (idempotent)
        channel.exchange_declare(
            exchange=exchange,
            exchange_type='direct',
            durable=True
        )

        # Create message properties
        correlation_id = str(uuid.uuid4())
        properties = pika.BasicProperties(
            headers={
                'IntegrationName': integration_name
            },
            correlation_id=correlation_id,
            content_type='application/json',
            delivery_mode=2  # Persistent
        )

        # Publish message
        body = json.dumps(message)
        channel.basic_publish(
            exchange=exchange,
            routing_key=routing_key,
            body=body,
            properties=properties
        )

        print(f"✓ Message published successfully!")
        print(f"  Correlation ID: {correlation_id}")
        print(f"\nNext steps:")
        print(f"  1. Check QuickApiMapper logs for processing details")
        print(f"  2. View captured messages via management API")
        print(f"  3. Check dead-letter queue if failed: {routing_key}.dead-letter")
        print()

        # Close connection
        connection.close()

    except Exception as e:
        print(f"\n✗ Error publishing message: {e}\n")
        raise

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description='Publish test messages to RabbitMQ for QuickApiMapper'
    )
    parser.add_argument(
        '--type',
        choices=['customer', 'order'],
        required=True,
        help='Type of message to publish'
    )
    parser.add_argument(
        '--integration',
        help='Integration name (default: derived from type)'
    )
    parser.add_argument(
        '--exchange',
        default='quickapi.exchange',
        help='RabbitMQ exchange name'
    )
    parser.add_argument(
        '--routing-key',
        help='RabbitMQ routing key (default: derived from type)'
    )
    parser.add_argument(
        '--host',
        default='localhost',
        help='RabbitMQ hostname'
    )
    parser.add_argument(
        '--port',
        type=int,
        default=5672,
        help='RabbitMQ port'
    )
    parser.add_argument(
        '--username',
        default='guest',
        help='RabbitMQ username'
    )
    parser.add_argument(
        '--password',
        default='guest',
        help='RabbitMQ password'
    )

    args = parser.parse_args()

    publish_message(
        message_type=args.type,
        integration_name=args.integration,
        exchange=args.exchange,
        routing_key=args.routing_key,
        hostname=args.host,
        port=args.port,
        username=args.username,
        password=args.password
    )

if __name__ == '__main__':
    main()
