#! /bin/bash
GH_USER=$(cat /deployment/secrets/github_org_user)
GH_PASS=$(cat /deployment/secrets/github_org_token)

GH_URL="github.com/DevOps-GroupF/server-files.git"
CLONE_PATH="/tmp/server-files/"
SCRIPT_PATH="./deploy.sh"

echo "----Cloning server repo...----"
rm -rf $CLONE_PATH
git clone https://$GH_USER:$GH_PASS@$GH_URL $CLONE_PATH
cd $CLONE_PATH

echo "----Executing deploy.sh script----"
sh $SCRIPT_PATH

echo "----Deploy script finished----"
exit 0
