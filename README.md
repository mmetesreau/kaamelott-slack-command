# Kaamelott slack command

Slack command to send kaamelott soundtracks.

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/screenshot.gif" width=70%></p>

## Installation

### Slack

Go to [https://api.slack.com/apps](https://api.slack.com/apps) and sign in to your Slack account.

Create a new app by clicking the ```Create New App``` button:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step1.png" width=50%></p>

Fill in the required information:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step2.png" width=50%></p>

Click on ```Slash Commands``` and create a new command by clicking the ```Create New Command button```. Then fill in the required information:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step4.png" width=50%></p>

*As the application endpoint which listen for commands is /command, the ```Request URL``` should be ```http[s]://[application]/command```*

Click on ```Interactivity & Shortcuts``` and turn on ```Interactivity```. Then fill in the required information:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step6.png" width=50%></p>

*As the application endpoint which listen for actions is /action, the ```Request URL``` should be ```http[s]://[application]/action```*

Click on ```OAuth & Permissions``` and add the ```files:write Upload, edit, and delete files on a user’s behalf``` in ```User Token Scopes```:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step8.png" width=50%></p>

Install the application to your workspace by clicking the ```Install App / Install To Workspace``` button:

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step9.png" width=50%></p>

If you want, you can also customize the display information.

<p align="center"><img src="https://github.com/mmetesreau/kaamelott-slack-command/raw/master/img/step10.png" width=50%></p>

Last but no least, the application will need the ```OAuth Access Token``` and the ```Verification Token``` to run.

*The ```Verification Token``` could be found in the ```Basic Information``` page under the ```App Credentials``` section*
*The ```OAuth Access Token``` could be found in the ```OAuth & Permissions``` page under the ```OAuth Tokens & Redirect URLs``` section*

## Run

Requirements:

- .NET 6

Powershell:
```
$env:ACCESSTOKEN="[yourslackaccesstoken]"
$env:VERIFICATIONTOKEN="[yourslackverificationtoken]"
dotnet restore
dotnet fsi .\src\app.fsx
```

Bash:
```
export ACCESSTOKEN=[yourslackaccesstoken]
export VERIFICATIONTOKEN=[yourslackverificationtoken]
dotnet restore
dotnet fsi .\src\app.fsx
```

## Using Docker

```
docker build -t kaamelottslackcommand .
docker run -d -p 8080:[your_port] --env ACCESSTOKEN=[yourslackaccesstoken] --env VERIFICATIONTOKEN=[yourslackverificationtoken] kaamelottslackcommand
```

## Deploy to Heroku

You can deploy this to Heroku by clicking this button:

[![Deploy to Heroku](https://www.herokucdn.com/deploy/button.png)](https://heroku.com/deploy)

Don't forget to set the ```ACCESSTOKEN``` and ```VERIFICATIONTOKEN``` config vars.
