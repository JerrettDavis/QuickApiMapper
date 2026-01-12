#!/bin/bash
# RabbitMQ initialization script for QuickApiMapper
# This script sets up exchanges, queues, and bindings for testing

set -e

echo "Waiting for RabbitMQ to be ready..."
sleep 10

echo "Setting up QuickApiMapper RabbitMQ configuration..."

# Declare exchanges
rabbitmqadmin declare exchange name=quickapi.exchange type=direct durable=true

# Declare input queues with dead-letter exchange configuration
rabbitmqadmin declare queue name=quickapi.customer.input durable=true \
  arguments='{"x-dead-letter-exchange":"quickapi.customer.input.dlx"}'

rabbitmqadmin declare queue name=quickapi.order.input durable=true \
  arguments='{"x-dead-letter-exchange":"quickapi.order.input.dlx"}'

# Declare dead-letter exchanges
rabbitmqadmin declare exchange name=quickapi.customer.input.dlx type=direct durable=true
rabbitmqadmin declare exchange name=quickapi.order.input.dlx type=direct durable=true

# Declare dead-letter queues
rabbitmqadmin declare queue name=quickapi.customer.input.dead-letter durable=true
rabbitmqadmin declare queue name=quickapi.order.input.dead-letter durable=true

# Bind input queues to exchange
rabbitmqadmin declare binding source=quickapi.exchange \
  destination=quickapi.customer.input routing_key=customer.created

rabbitmqadmin declare binding source=quickapi.exchange \
  destination=quickapi.order.input routing_key=order.created

# Bind dead-letter queues to dead-letter exchanges
rabbitmqadmin declare binding source=quickapi.customer.input.dlx \
  destination=quickapi.customer.input.dead-letter routing_key=quickapi.customer.input

rabbitmqadmin declare binding source=quickapi.order.input.dlx \
  destination=quickapi.order.input.dead-letter routing_key=quickapi.order.input

echo "RabbitMQ configuration complete!"
echo ""
echo "Created resources:"
echo "  - Exchange: quickapi.exchange"
echo "  - Queue: quickapi.customer.input (with DLX)"
echo "  - Queue: quickapi.order.input (with DLX)"
echo "  - Dead-letter queues for both"
echo ""
echo "Access management UI at: http://localhost:15672"
echo "Username: guest"
echo "Password: guest"
