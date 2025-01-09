# Remote Webcam Control Server

This is a tool to control the webcam properties using a webserver. Making it possible to easily control webcam settings from somthing like [Streamerbot](https://streamer.bot/)

## Using the server

Currently there are only 2 endpoints.

GET "/GetCamera/{cameraName}"
POST "/SetCamera"

`/GetCamera/` requires the last part to be your cameras name (caseinsenstive"
`http://localhost:555/GetCamera/Cam1` `Cam1`would be the name of the camera as it appears in obs.

`/SetCamera` requires the body of the POST request to be in json format. The format can be found [here](https://github.com/DerrikCreates/Remote-Camera-Control/blob/master/CameraServer/SetCameraMessage.cs)

here is a valid json string `{"name":"Cam1","focus":198,"exposure":-7,"brightness":110,"contrast":128,"satuation":128,"sharpness":128,"whiteBalance":7500,"backlightComp":1,"gain":120}`

not every field is required, the server only processes the fields provided.
If your request was successful then the server will respond with a status code of `200 OK`.
If it failed it will respond with a `400 Bad Request`, details on included in the response body.

## Renaming cameras

The server requires you to provide the friendly name of a camera. This can be a problem if you have many of the same camera. Changing the name isnt hard.

1. Download [Registry Finder](https://registry-finder.com/) or use the windows built in regedit

2. Run the camera server. When the server starts find the current name of the camera you want to change. On the line below it will give you some text that looks like this `@device:pnp:\\?\usb#vid_1532&pid_0e03&mi_00#6&1c7dc219&2&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global`

3. copy the section in bold @device:pnp:\\?\\**usb#vid_1532&pid_0e03&mi_00#6&1c7dc219&2&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}**\global for me the text copied will be `usb#vid_1532&pid_0e03&mi_00#6&1c7dc219&2&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global` we **dont** want the text to the left of `usb` or the `\global` at the end

4. now in Registry Finder or regedit, search for this text. You can search your entire regsitry, if find multiple results the correct result starts with `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\`

inside of this there should be a `#GLOBAL` folder and inside it a `Device Parameters` folder.
![regkey](./regkey.png)

From here change the `FriendlyName` to a new name. Then restart your webcam app and the new name should appear.
