# Loop

# Unity Version 
2019.4.28f1

# Scene
Name the main scene as "year_month_date"
for example scene made in  2021/7/17 should be named 21_7_17

# Perspective
Camera Rotation :  x=-20, y=0, z=0
Object Rotation :  x=-30, y=0, z=0 (only rotation object sprite, not object itself)

# Add new Network Object
When adding new network object, remember to assign it to the NetworkManagerMMO Object in the main scene. (Registered Spawnable Prefabs)

# Sprite Shadow
set pivot to the buttom
change rotation to x=-30, y=0, z=0
change material to "SpriteShadow"
Go to debug mode in Inspector, change CastShadow to "On"

# PlayTesting
1. Host & play
2. Server Only + 1 Client
3. Server Only + 2 Clients (focus on the 2 clients)

# Database
You can use DB browser or any type of SQLite browser to open& edit Database.sqlite
