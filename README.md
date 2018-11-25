#GCVTest - Google Computer Vision Tester

The application utilizes the Google Computer Vision API in order to perform OCR functions on pictures of vehicles with their license plates.

#INSTRUCTIONS

Installation Instructions:

1). Login to Google Compute console and create the service account key for Computer Vision API, download the json file

2). Run Powershell command

C:\> [Environment]::SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "$env:USERPROFILE\Documen
ts\GCVtest-api-credentials.json", "User") 

3). Run the VS2015 Project

#PRE-REQUISITES

Microsoft .Net Framework 4.6 Client Profile
Visual Studio 2015
Google Cloud Computer Computer Vision API login credentials

#ADDITIONAL INFORMATION

https://cloud.google.com/vision/
https://cloud.google.com/vision/docs/detecting-text#vision-text-detection-gcs-csharp
