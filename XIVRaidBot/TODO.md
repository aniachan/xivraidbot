# To Do

## Deployment and CI/CD
Create kubernetes manifests and deploy to cloud cluster

Create GitHub workflows to build, test, and deploy the application into kube-configs by pushing manifests into a remote repo.

## Testing
Implement integration testing so that we can automatically spin up a discord bot and test all of its functionality. 

## Prompts
I want you to implement permissions where a server admin can delegate a certain role as an editor role who is allowed to schedule raids and configure settings. Regular users can only do things like join an existing raid. 

I want you to implement scheduled raids. The idea is that a user can create a raid which is scheduled on certain days (for example, on Wednesdays and Fridays at 19:00, lasting 2 hours). This schedule should then remember who is a member of the raid statically and automatically create a new raid with these people pre-populated. 

Raids should remind players 1 day before to confirm attendance by alerting a configurable discord server role.

Raids should also alert players who confirmed they will attend 30 minutes before the raid takes place by pinging them directly. 

They will be required to confirm attendance just like normal, and if they say they aren't available, someone from the bench can be used.