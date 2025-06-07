# To Do

## Features
- Implement the job icons (reference githubusercontent endpoints for icon urls)

## Cleanliness
- Replace console logging with a proper logging library

## Deployment and CI/CD
- Implement configuration reading from environment variables (for use in kubernetes)
- Containerise the application in a dockerfile (must support arm64)
- Create kubernetes manifests and deploy to cloud cluster
- Create GitHub workflows to build, test, and deploy the application into kube-configs via argocd
