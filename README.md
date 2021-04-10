# ParkingAI
This is a school project: an AI car learning to park itself, using Unity and ML-Agents. We made it into a game (Player vs. AI).

This project was inspired by the following YouTube video: https://www.youtube.com/watch?v=VMp6pq6_QjI (AI Learns to Park - Deep Reinforcement Learning)

## Instructions
In the Build/ folder you can run the game for Windows or MacOS. Or you can open the solution in Unity.

There are 3 scenes: GameManager which is the game itself, BasicTraining and ParkingTraining.

## Training
We used two training scenes: one in a simpler environment (BasicTraining), and one in the parking lot (ParkingTraining).

We started from the RollerBall example of ML-Agents. We modified the Agent to turn it into a car, and used the observations and configuration file provided in the following article: https://auro.ai/blog/2020/05/learning-to-drive/ . We added Ray Perception Sensors to the car, and set the reward for reaching the target to +1.0.

We first trained the agent to find a target:
![](/Media/target-training.gif)(speed x20)

We then replaced the target with our parking spot, progressively increasing the constraints to receive the reward: to be considered parked in the game, the car must be less than 0.3 away and the angle bewteen the car and the parking spot must be less than 15°. So we started with (distance, angle) : (1.0, 45), then decreased to  (0.75, 30), (0.5, 20), and finally (0.3, 15).
![](/Media/spot-training.gif)(speed x20)

We then added obstacles, with -1.0 reward (and EndEpisode) if the car collides with one. The obstacles are the other cars and the walls.
![](/Media/obstacles-training.gif)(speed x20)

Once the mean reward reached a plateau, we changed the environment to the parking lot and trained it further:
![](/Media/parking-training.gif)(speed x20)

We reached a mean cumulative reward of ~0.88 .
![](/Media/environment-cumulative-reward.png)

## Game
We added our trained AI to the game, and it was pretty decent.
![](/Media/game-1.gif)
![](/Media/game-2.gif)

We could probably increase its performance by modifying the configuration file and increasing the length of the car sensors.
