using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ParamLogger
{
    public class Functions
    {
        // This const is the name of the environment variable that the serverless.template will use to set
        // the name of the DynamoDB table used to store blog posts.
        const string TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP = "EventTable";

        public const string ID_QUERY_STRING_NAME = "Id";
        IDynamoDBContext DDBContext { get; set; }

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            // Check to see if a table name was passed in through environment variables and if so 
            // add the table mapping.
            var tableName = System.Environment.GetEnvironmentVariable(TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP);
            if(!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(RequestEvent)] = new Amazon.Util.TypeMapping(typeof(RequestEvent), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        /// <summary>
        /// Constructor used for testing passing in a preconfigured DynamoDB client.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        public Functions(IAmazonDynamoDB ddbClient, string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(RequestEvent)] = new Amazon.Util.TypeMapping(typeof(RequestEvent), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(ddbClient, config);
        }

        public async Task<APIGatewayProxyResponse> AddEventAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var eventToLog = new RequestEvent
            {
                Id = Guid.NewGuid().ToString(),
                CreatedTimestamp = DateTime.Now,
                Body = request?.Body,
                Path = request?.Path,
                SourceIp = request?.RequestContext.Identity.SourceIp,
                UserAgent = request?.RequestContext.Identity.UserAgent
            };
            if (request?.QueryStringParameters.Keys.Count > 0)
            {
                eventToLog.QueryParameters = new Dictionary<string, string>(request?.QueryStringParameters);
            }

            if (request?.PathParameters.Keys.Count > 0)
            {
                eventToLog.PathParameters = new Dictionary<string, string>(request?.PathParameters);
            }

            

            context.Logger.LogLine($"Saving event with id {eventToLog.Id}");
            await DDBContext.SaveAsync<RequestEvent>(eventToLog);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAQAAAC1+jfqAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAAAmJLR0QA/4ePzL8AAAAJcEhZcwAADsQAAA7EAZUrDhsAAAAHdElNRQfhCgYKDg99vJAzAAABKElEQVQozz3Rz0pbYRAF8F++5GoSYlvxitULCmZTMApuBBcuXYi7btz5Bn0Cn8l36KoFQUH8Q6HVhZBQQ0Ci0Wiqpov7cWczzMxhzjkzJXnUtGRetPT8Nant3BDKYM6mO0d+2dJ36MZHax48iuNdKaZtuXBtT0Bq11y+fEeK4Jt9P431fAWpHbVgVUcPiWVX/mHGgc/o6VgNMn9AXXCvBFasgytZMIpSpnRNGYNJX8DAKHiKRj/5LVWN1WLMTxUKQNvIbKwywTsE9dj64N6l2wKe5MqCCQ3Q0Df2Vly2goaJoK2JoGoQPUAioKkdnFmQSpQLuTl1SWrBWcXQiQ3XqpYMhQgom9d0bFjGo75tLfPquo79cOrZyHddBWvNisyrgbG6RMdl/u7/jNpNflq9fc4AAAAldEVYdGRhdGU6Y3JlYXRlADIwMTctMTAtMDZUMTA6MTQ6MTUrMDI6MDBIp0pTAAAAJXRFWHRkYXRlOm1vZGlmeQAyMDE3LTEwLTA2VDEwOjE0OjE1KzAyOjAwOfry7wAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAAASUVORK5CYII=",
                Headers = new Dictionary<string, string> { { "Content-Type", "image/png" } }
            };
            return response;
        }
    }
}
