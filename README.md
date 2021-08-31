# Samples.JwtAuth
Sample api for displaying some fairly straightforward JWT authentication that attempts to follow as closely to a PCI compliant login/auth process as is possible in a locally deployed system.

## [Clone repo](#clone-repo)

```bash
git clone https://github.com/boydc7/Samples.JwtAuth
cd Samples.JwtAuth
```

## [Docker Run](#docker-run)
```bash
docker-compose up
```

NOTE: To rebuild and force recreation of the various containers:
```bash
docker-compose up --build --force-recreate --renew-anon-volumes
```

## [Build Test Run](#build-test-run)
If you prefer to run from the source code/ide directly, you can install .NET Core and/or an IDE and run from there, or the cmd line:

NOTE: This is not needed for running the server only, you can simply use the Docker run approach above.

* Download and install dotnet core 5.0
  * Mac: <https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-macos-x64-installer>

  * Linux instructions: <https://docs.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website>

  * Windows: <https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x64-installer>

* Clone code and build:
```bash
git clone https://github.com/sourcetable/Samples.JwtAuth
cd Samples.JwtAuth
dotnet publish -c Release -o publish Samples.JwtAuth.Api/Samples.JwtAuth.Api.csproj
```
* Run (from Samples.JwtAuth folder from above)
```bash
cd publish
dotnet authapi.dll
```
* Set the following environment variables in your IDE configuration, launch configuration, shell, etc.:
  * NOTE: You can feel free to use the same values defined in the docker-compose.yml file in the root of the repo if you do not have/want to customize your own.

  * AWS_ACCESS_KEY_ID
    * Required if you do not set AWS_PROFILE
    * To your programatic AWS access key id.
  * AWS_SECRET_ACCESS_KEY
    * Required if you do not set AWS_PROFILE
  * AWS_PROFILE
    * Required if you do not set the above 2 keys, OR if you have a conflicting default AWS Profile set via AWS command line tools/etc.
    * Name of the AWS profile configured on your machine with creds to be used
  * SMP_AUTHAPI__AWS__DYNAMO__SERVICEURL
    * Required if not running inside docker and using a localdynamo installation as the source for DynamoDb (which would be common if you are developing locally and do not want to connect directly to a DynamoDb instance in AWS).
    * As an example, if you were to start the auth-dynamo service only in the docker compose and wanted to connect to that from your IDE, you'd likely set this to something like:
      * http://localhost:8085

## Secrets
For AWS related credentials, you can use any method of storing secrets/credentials that are [supported by the AWS .NET SDK](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html).  If you use one of these methods, the AWS related ENV vars do not need to be set.

Can be set locally via environment variables or other non-source-controlled method, the required secrets map to the same vars above in the steps to do local development.
  
## Technology and Choices, Tradeoffs

On the design front I certainly cut some corners here on separating abstractions and implementation libraries - I've
mingled everything into a single project for simplicity sake. In "normal" circumstances I'd have a project structure
likely similar to the following:

* Samples.Api.JwtAuth
* Samples.Service.Identity
* Samples.Identity
* Samples.Identity.Abstractions
* Samples.DynamoDb
* Samples.Identity.DynamoDb ... (etc.)

I'm using a [bcrypt](https://en.wikipedia.org/wiki/Bcrypt) implementation for the password hashing and storage (bcrypt internally uses a variant of [Blowfish](https://en.wikipedia.org/wiki/Blowfish_(cipher)) internally) along with a SHA512 hashing algorithm and higher than default work factor - I don't really care if my password hashing takes a long time to run (you could argue that's a good idea).

I'm using DynamoDb as the storage engine - it's fast, simple, flexible, secure (in production), and hosted for you (in production). And it fits the bill quite nicely for something as simple as auth (simple in the data structure sense).

I did NOT implement all the proper RefreshToken parent/descendant invalidation at what would normally be recommended locations...I would in a "real" implementation.

I also did not choose to implement email verification on signup or on password change, which would normally be included in a "real" implementation.

There is also no logging/auditing of changes to auth records, which is something else that would certainly be added in a "real" service, and could (should) be here as well, I just ran out of time alloted.

## NOTES

The api will start and by default listen for requests on localhost:8084.

Once started, you can view the Swagger for the API at <http://localhost:8084>

## [Endpoints](#endpoints)

See swagger at <http://localhost:8084> after starting the API, from where you can view endpoints and execute requests/get results inline.

You can also access a POSTMAN collection that excercises the endpoints here:

<https://www.getpostman.com/collections/19a6755494c7d961a100>
