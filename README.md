# V-Assist
 
V-Assist is a basic Discord bot meant for tracking the usage of narrative points for the fan-based Gundam tabletop system "Operation V." 

# Commands
- /narrative-point-tracker <narrative_points> [session_name]
	- Will create a tracker with the specified number of narrative points. If session_name is specified, it will be created under that name. 
	- Each tracker will give the option to spend a point, add a point, add reasons for point changes, or end the session. 
	- Each tracker is able to have up to 24 point changes before a new tracker is required.
	- An individual can only add reasons for the previous 5 changes they are responsible for.