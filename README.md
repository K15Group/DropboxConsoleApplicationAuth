# DropboxConsoleApplicationAuth
Dropbox Setup
Create Dropbox application

Go to Application → Permissions and select all Files and Folders permissions
![image](https://user-images.githubusercontent.com/32443176/162875504-31e1b90b-dc16-4548-8adc-44659337334c.png)
Go to Settings. Note the App key and App secret. Then set OAuth 2 → Redirect URIs. These values to get access token. The Redirect URI just need a localhost with an unused port. For the current code structure, the URI should have ending authorize
![image](https://user-images.githubusercontent.com/32443176/162875521-cc88ed09-e707-42d5-a0d6-b3b3574ded32.png)
Note that for the Dropbox no support long-lived token now. Otherwise, the basic authentication could not be used to list and download files
