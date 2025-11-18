#!/bin/bash

ENDPOINT_URL=http://localhost:4566

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
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name trade_imports_data_upserted_decision_deriver
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name trade_imports_data_upserted_decision_deriver-deadletter

aws --endpoint-url=http://localhost:4566 sqs set-queue-attributes --queue-url "http://localhost:4566/000000000000/trade_imports_data_upserted_decision_deriver" --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_decision_deriver-deadletter\",\"maxReceiveCount\":\"1\"}"}'


function is_ready() {
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_decision_deriver || return 1
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_decision_deriver-deadletter || return 1
    return 0
}

while ! is_ready; do
    echo "Waiting until ready"
    sleep 1
done

touch /tmp/ready
