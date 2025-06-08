# To Do

## Features
- Need to convert timezone to UTC when receiving timestamps from users. 
	 Might also need to store the timezone of the user somewhere for easy 
     conversion. Also make sure to use the discord local time formatting 
     thing for printing it back for everyone to see.

## Deployment and CI/CD
- Implement configuration reading from environment variables (for use in kubernetes)
- Containerise the application in a dockerfile (must support arm64)
- Create kubernetes manifests and deploy to cloud cluster
- Create GitHub workflows to build, test, and deploy the application into kube-configs via argocd

## Testing
- Look into a way to do e2e testing of the bot (see https://stackoverflow.com/questions/67306776/how-to-write-unit-tests-for-discord-bot)
