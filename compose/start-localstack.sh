#!/bin/bash

ENDPOINT_URL=http://sqs.eu-west-2.localhost.localstack.cloud:4566

export AWS_ENDPOINT_URL=$ENDPOINT_URL
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

# S3 buckets
# aws --endpoint-url=http://localhost:4566 s3 mb s3://my-bucket

# SQS queues
# aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name my-queue

# SQS queues
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name data_events
