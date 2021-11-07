## Introduction

When using ProblemDetails, the default behaviour when an validation error occures is to return an response that shows what went wrong.

Given the following action method:

```
public IEnumerable<WeatherForecast> Get([FromQuery]DateTime date)
{
	return Enumerable.Range(1, 5).Select(index => new WeatherForecast
	{
		Date = DateTime.Now.AddDays(index),
		TemperatureC = Random.Shared.Next(-20, 55),
		Summary = Summaries[Random.Shared.Next(Summaries.Length)]
	})
	.ToArray();
}
```

When executing the following request:
```
GET /WeatherForecast?date=josef
```

The following response will be returned.

```
{
	"type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
	"title": "One or more validation errors occurred.",
	"status": 400,
	"traceId": "00-a6671681df84f418db994be30c3e2689-ea61fab32a4d3917-00",
	"errors": {
		"date": ["The value 'josef' is not valid."]
	}
}
```

Now. Imagine that our client code that handles the response just prints out the error, something like this:

```
function jqueryExploit(){
  var dateValue = $('#date-input').val();
  var url = "https://localhost:7135/WeatherForecast?date=" + dateValue;
$( "#success" ).load( url, function( response, status, xhr ) {
  if ( status == "error" ) {
    var problemDetails = JSON.parse(response);
    var dateError = problemDetails.errors.date;
    $( "#errors" ).html("<h1>Errors occured</h1>"+dateError );
  }
});

}
```

Now imagine that the input entered in the input field was something like ```2021-10-28<script>alert('you%20have%20been%20hacked')</script>```...
It would trigger an alert.

## Reproduce
1. Start the web application on port 7135 (https). ```dotnet run``` in the **WebApplication2** folder.
2. Open the attached index.html in a browser
3. For jquery, paste in ```2021-10-28<script>alert('you%20have%20been%20hacked')</script>``` in the text input and press "jQuery exploit"
4. For "vanilla", paste in ```2021-10-28 <a href='javascript:alert("you have been hacked");'> Click to fix problem</a>``` and press on "Vanilla...".

## Question
Should the framework be responsible for escaping the user input instead of just blindly return it back in the problem details response?

## More information
[https://josef.codes/beware-of-potential-xss-injections-when-using-problemdetails-in-asp-net-core/](https://josef.codes/beware-of-potential-xss-injections-when-using-problemdetails-in-asp-net-core/)