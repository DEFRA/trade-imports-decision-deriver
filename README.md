# trade-imports-decision-deriver

The Trade Imports Decision Deriver is a .NET application which subscribes to the Trade Import Api Events.

When events are received the Decision Deriver will determine if there is enough information to derive a decision, and if it does, it will send the Decision back to the Trade Imports API via a Http Request.




* [Prerequisites](#prerequisites) 
* [Setup Process](#setup-process)
* [How to run in development](#how-to-run-in-development)
* [How to run Tests](#how-to-run-tests)
* [Running](#running)
* [Deploying](#deploying)
* [SonarCloud](#sonarCloud)
* [Dependabot](#dependabot)
* [Message Consumption](#message-consumption)
* [Tracing](#tracing)
* [Licence Information](#licence-information)
* [About the Licence](#about-the-licence)

### Prerequisites

- .NET 9 SDK
- Docker
  - localstack - used for local queuing
  - wiremock - used for mocking out http requests 
  

### Setup Process

- Install the .NET 9 SDK
- Install Docker
  - Run the following Docker Compose to set up locally running queues for testing
  ```bash
  docker compose -f compose.yml up -d
  ```

### How to run in development

Run the application with the command:

```bash
dotnet run --project BtmsGateway --launch-profile BtmsGateway
```

### How to run Tests

Run the unit tests with:

```bash
dotnet test --filter "Category!=IntegrationTest"
```
Run the integration tests with:
```bash
dotnet test --filter "Category=IntegrationTest"
```
Run all tests with:
```bash
dotnet test 
```

#### Unit Tests
Some unit tests may run an in memory instance service. 

Unit tests that need to edit the value of an application setting can be done via the`appsettings.IntegrationTests.json`

#### Integration Tests
Integration tests run against the built docker image.

Because these use the built docker image, the `appsettings.json` will be used, should any values need to be overridden, then they can be injected as an environment variable via the`compose.yml`

### Deploying

Before deploying via CDP set the correct config for the environment as per the `appsettings.Development.json`.

### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

### Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`

### Message Consumption
This service using a framework called [Slim Message Bus](https://github.com/zarusz/SlimMessageBus), which maps queues/types to consumer classes.

The Decision Deriver is only interested in a sub set of the Trade Import Api Events:

 - `ResourceType` equals `ImportPreNotification`
 - `ResourceType` equals `CustomsDeclaration` and `ResourceSubType` equals
   `ClearanceRequest`

This filtering happens on the SQS/SNS subscription.

SMB doesn't support [filtering on message headers](https://github.com/zarusz/SlimMessageBus/issues/402), so there is a custom `ConsumerMediator` class that has been implemented to facilitate this.

### Tracing
The out of the box CDP template doesn't provide any example of how to handle tracing for non Http communication.
This service expected the trace.id to be a header on the message, and it will use that and propagate that to the Trade Imports Api via Http Requests.

Getting the trace.id header is achieved via a SMB `TraceContextInterceptor`
Making sure that trace.id is then used in log messages is achieved via `TraceContextEnricher`
Setting the trace.id header on Http Request is achieved via Header Propagation


### Licence Information

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.


