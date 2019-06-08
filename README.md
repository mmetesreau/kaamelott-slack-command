# Kaamelott slack command

Slack command to send kaamelott soundtracks.

![example](/img/screenshot.gif "example")

## Technical Instructions

### Slack

Go to [https://api.slack.com/apps](https://api.slack.com/apps) and sign in to your Slack account.

Create a new app by clicking the ```Create New App``` button.

![step1](/img/step1.png "step1")

Fill in the required information.

![step2](/img/step2.png "step2")

Click on ```Slash Commands```.

![step3](/img/step3.png "step3")

Create a new command by clicking the ```Create New Command button```. Fill in the required information.

![step4](/img/step4.png "step4")

As the application endpoint which listen for commands is /command, the ```Request URL``` should be ```http[s]://[application]/command```.

Click on ```Interactive Components```.

![step5](/img/step5.png "step5")

Turn on ```Interactivity```. Fill in the required information.

![step6](/img/step6.png "step6")

As the application endpoint which listen for actions is /action, the ```Request URL``` should be ```http[s]://[application]/action```.

Click on ```Permissions```.

![step7](/img/step7.png "step7")

Add the ```Upload and modify files as user``` in ```Scopes```.

![step8](/img/step8.png "step8")

It is needed to upload the sounds directly to Slack.

Install the application to your workspace by clicking the ```Install App To Workspace``` button.

![step9](/img/step9.png "step9")

If you want, you can also customize the display information.

![step10](/img/step10.png "step10")

Last but no least, the application will need the ```OAuth Access Token``` and the ```Verification Token``` to run.

- The ```Verification Token``` could be found in the ```Basic Information``` page under the ```App Credentials``` section.
- The ```OAuth Access Token``` could be found in the ```OAuth & Permissions``` page under the ```OAuth Tokens & Redirect URLs``` section.

### Requirements

- F#

### Run

```
$env:ACCESSTOKEN="[yourslackaccesstoken]"
$env:VERIFICATIONTOKEN="[yourslackverificationtoken]"
.\paket\paket.bootstrapper.exe
.\paket\paket.exe restore
fsi.exe .\src\app.fsx
```

### Using Docker

```
docker build -t kaamelotslackcommand .
docker run -d -p 8080:[your_port] --env ACCESSTOKEN=[yourslackaccesstoken] --env VERIFICATIONTOKEN=[yourslackverificationtoken] kaamelottslackcommand
```