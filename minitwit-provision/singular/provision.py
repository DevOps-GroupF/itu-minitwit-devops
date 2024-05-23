import os
import io
from time import sleep
from urllib.parse import urlparse, parse_qs
from pydo import Client
from fabric import Connection
from dotenv import load_dotenv
import argparse

parser = argparse.ArgumentParser()
parser.add_argument("--skip_do", action="store_true", help="Skip creating a vm")

args = parser.parse_args()

REGION = "fra1"
SIZE = "s-1vcpu-1gb"
IMAGE = "ubuntu-23-10-x64"


def main():
    load_dotenv()

    ip_address = os.environ.get("IP_ADDRESS")

    env_token = os.environ.get("DIGITALOCEAN_TOKEN")
    check_env_var(env_token, "DIGITALOCEAN_TOKEN")

    do_db_name = os.environ.get("DO_DB_NAME")
    check_env_var(do_db_name, "DO_DB_NAME")

    vm_name = os.environ.get("VM_NAME")
    check_env_var(vm_name, "VM_NAME")

    ssh_key_name = os.environ.get("SSH_KEY_NAME")

    if ssh_key_name == "" or ssh_key_name == None:
        ssh_key_name = f"ssh-key-{vm_name}"
        print(
            f'No ssh key name supplied in env var SSH_KEY_NAME. Using the name "{ssh_key_name}" instead.'
        )

    ssh_public_key = os.environ.get("SSH_PUBLIC_KEY")
    check_env_var(ssh_public_key, "SSH_DEPLOY_PUBLIC_KEY")

    ssh_private_key_path = os.environ.get("SSH_PRIVATE_KEY_PATH")
    check_env_var(ssh_private_key_path, "SSH_PRIVATE_KEY_PATH")

    if not os.path.isfile(ssh_private_key_path):
        raise Exception(
            "Path specified by SSH_PRIVATE_KEY_PATH does not point to a file"
        )

    ssh_deploy_public_key = os.environ.get("SSH_DEPLOY_PUBLIC_KEY")
    check_env_var(ssh_deploy_public_key, "SSH_DEPLOY_PUBLIC_KEY")

    ssh_deploy_private_key_path = os.environ.get("SSH_DEPLOY_PRIVATE_KEY_PATH")
    check_env_var(ssh_deploy_private_key_path, "SSH_DEPLOY_PRIVATE_KEY_PATH")

    if not os.path.isfile(ssh_deploy_private_key_path):
        raise Exception(
            "Path specified by SSH_DEPLOY_PRIVATE_KEY_PATH does not point to a file"
        )

    admin_username = os.environ.get("ADMIN_USERNAME")
    check_env_var(admin_username, "ADMIN_USERNAME")

    admin_password = os.environ.get("ADMIN_PASSWORD")
    check_env_var(admin_password, "ADMIN_PASSWORD")

    db_conn_string = os.environ.get("DB_CONN_STRING")
    check_env_var(db_conn_string, "DB_CONN_STRING")

    db_conn_password = os.environ.get("DB_CONN_PASSWORD")
    check_env_var(db_conn_password, "DB_CONN_PASSWORD")

    db_conn_url = os.environ.get("DB_CONN_URL")
    check_env_var(db_conn_url, "DB_CONN_URL")

    db_conn_user = os.environ.get("DB_CONN_USER")
    check_env_var(db_conn_user, "DB_CONN_USER")

    github_org_token = os.environ.get("GITHUB_ORG_TOKEN")
    check_env_var(github_org_token, "GITHUB_ORG_TOKEN")

    github_org_user = os.environ.get("GITHUB_ORG_USER")
    check_env_var(github_org_user, "GITHUB_ORG_USER")

    github_pck_token = os.environ.get("GITHUB_PCK_TOKEN")
    check_env_var(github_pck_token, "GITHUB_PCK_TOKEN")

    github_pck_user = os.environ.get("GITHUB_PCK_USER")
    check_env_var(github_pck_user, "GITHUB_PCK_USER")

    grafana_password = os.environ.get("GRAFANA_PASSWORD")
    check_env_var(grafana_password, "GRAFANA_PASSWORD")

    grafana_user_email = os.environ.get("GRAFANA_USER_EMAIL")
    check_env_var(grafana_user_email, "GRAFANA_USER_EMAIL")

    grafana_user_password = os.environ.get("GRAFANA_USER_PASSWORD")
    check_env_var(grafana_user_password, "GRAFANA_USER_PASSWORD")

    grafana_user_username = os.environ.get("GRAFANA_USER_USERNAME")
    check_env_var(grafana_user_username, "GRAFANA_USER_USERNAME")

    logging_env_path = os.environ.get("LOGGING_ENV_PATH")
    check_env_var(logging_env_path, "LOGGING_ENV_PATH")

    if not os.path.isfile(logging_env_path):
        raise Exception("Path specified by LOGGING_ENV_PATH does not point to a file")

    remote_script_path = os.environ.get("REMOTE_SCRIPT_PATH")
    check_env_var(remote_script_path, "REMOTE_SCRIPT_PATH")

    if not os.path.isfile(remote_script_path):
        raise Exception("Path specified by REMOTE_SCRIPT_PATH does not point to a file")

    sshd_config_path = os.environ.get("SSHD_CONFIG_PATH")
    check_env_var(sshd_config_path, "SSHD_CONFIG_PATH")

    if not os.path.isfile(sshd_config_path):
        raise Exception("Path specified by SSHD_CONFIG_PATH does not point to a file")

    if not args.skip_do:
        client = Client(token=env_token)
        account = None

        try:
            print("Trying to fetch DO account")
            account = client.account.get()
        except Exception as error:
            print(f"Error getting account from DO: {error}")
            raise Exception("Error connecting to DO")
        else:
            print("Successfully fetched account info for ", account["account"]["name"])

        print(f"Checking if a database with the name {do_db_name} exists.")

        db_response = client.databases.list_clusters()
        databases = db_response["databases"]

        if len(databases) == 0:
            raise Exception("No databases on account.")

        db_found = False
        db_found_id = None

        for db in databases:
            if db["name"] == do_db_name:
                if not db["status"] == "online":
                    raise Exception(
                        f"A DB with the name {do_db_name} was found, but it is not running."
                    )
                db_found = True
                db_found_id = db["id"]

        if db_found is False:
            raise Exception(f"Did not find a DB with the name {do_db_name}.")

        print(f"Found a DB with the name {do_db_name}. Proceeding with provisioning.")

        print(f"Checking if VM name {vm_name} is already in use")
        for droplet in client.droplets.list()["droplets"]:
            if droplet["name"] == vm_name:
                raise Exception("VM name already taken")

        print(f"VM name {vm_name} is not in use")
        # Create ssh key

        key_fingerprint = None

        try:
            print(f"Checking if an SSH key with name {ssh_key_name} already exists")
            key_fingerprint = find_ssh_key(client, ssh_key_name)
        except Exception:
            print(f"Key name {ssh_key_name} not in use, creating a key with that name")

            created_key = client.ssh_keys.create(
                body={"name": ssh_key_name, "public_key": ssh_public_key},
                content_type="application/json",
            )

            key_fingerprint = created_key["ssh_key"]["fingerprint"]
            print(f"Created an SSH key with fingerprint {key_fingerprint}")
        else:
            key_fingerprint = key_fingerprint["fingerprint"]
            print(
                f"An SSH key with name {ssh_key_name} already exists with fingprint {key_fingerprint}. Using that one for the new VM"
            )

        # Create Droplet

        droplet_creation_request_body = {
            "name": vm_name,
            "region": REGION,
            "size": SIZE,
            "image": IMAGE,
            "ssh_keys": [key_fingerprint],
        }

        print("Creating Droplet using: {0}".format(droplet_creation_request_body))

        response = client.droplets.create(body=droplet_creation_request_body)

        print("VM creation request sent.")

        droplet_creation_action_id = response["links"]["actions"][0]["id"]

        wait_for_action(client, droplet_creation_action_id)

        created_droplet = client.droplets.get(response["droplet"]["id"])
        created_droplet = created_droplet["droplet"]

        ip_address = None

        for net in created_droplet["networks"]["v4"]:
            if net["type"] == "public":
                ip_address = net["ip_address"]

        print("Created Droplet!")
        print("Droplet ID:", created_droplet["id"])
        print("Droplet name:", created_droplet["name"])
        print("Droplet IP addr:", ip_address)

        print(
            f'Getting all the firewall rules so the Droplet "{vm_name}" can be added to the allow-list.'
        )

        db_rules = client.databases.list_firewall_rules(
            database_cluster_uuid=db_found_id
        )

        new_rule = {"type": "droplet", "value": f'{created_droplet["id"]}'}
        db_rules["rules"].append(new_rule)

        print(db_rules)

        print(f'Adding the Droplet "{vm_name}" to the allow-list of DB "{do_db_name}".')

        client.databases.update_firewall_rules(
            database_cluster_uuid=db_found_id, body=db_rules
        )

        print(f'Added the Droplet "{vm_name}" to the allow-list of DB "{do_db_name}".')

    # Connect to VM and run provisioning

    print("Connecting to VM to begin provisioning.")

    c = Connection(
        host=ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    done = False

    while not done:
        try:
            c.run("echo Hello!")
        except Exception:
            print("Could not connect yet. Trying again in 3 seconds")
            sleep(3)
        else:
            done = True
            print("Successfully connected to VM.")

    #
    ## Update packages
    print("Updating packages")
    c.run(
        """apt-get update -yq && \
        NEEDRESTART_MODE=a \
        UCF_FORCE_CONFFOLD=1 \
        DEBIAN_FRONTEND=noninteractive \
        apt-get \
        -o Dpkg::Options::=--force-confold \
        -o Dpkg::Options::=--force-confdef \
        --allow-downgrades \
        --allow-remove-essential \
        --allow-change-held-packages \
        -yq \
        upgrade"""
    )

    print("Attempting to restart VM.")
    try:
        c.run(f"shutdown -r now")
    except Exception:
        print("Called restart")

    c.close()

    print("Waiting for 5 seconds to let the VM restart.")
    sleep(5)

    print("Attempting to reconnect to VM.")
    c = Connection(
        host=ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
        connect_timeout=120,
    )

    done = False

    while not done:
        try:
            c.run("echo Hello!")
        except Exception:
            print("Could not connect yet. Trying again in 3 seconds")
            sleep(3)
        else:
            print("Successfully reconnected to VM.")
            done = True

    ## Install Docker

    c.run(
        "for pkg in docker.io docker-doc docker-compose docker-compose-v2 podman-docker containerd runc; do apt-get remove $pkg -yq; done"
    )

    print("Begin installing docker")
    ### Add Docker's official GPG key:
    c.run("apt-get update -yq")
    c.run("apt-get install -yq ca-certificates curl")
    c.run("install -m 0755 -d /etc/apt/keyrings")
    c.run(
        "curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc"
    )
    c.run("chmod a+r /etc/apt/keyrings/docker.asc")
    ### Add the repository to Apt sources:
    c.run(
        'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null'
    )
    c.run("apt-get update -yq")
    c.run(
        "apt-get install -yq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin"
    )
    c.run("sudo systemctl enable docker.service")
    c.run("sudo systemctl enable containerd.service")

    print("Docker successfully installed")

    print(f"Setting up admin account with username {admin_username}")
    ## Setup admin account
    c.run(f"useradd -m {admin_username}")
    c.run(f"adduser {admin_username} sudo")
    # c.run(f"usermod --password {admin_password} {admin_username}")
    # c.run(
    #     f"usermod --password $(echo {admin_password} | openssl passwd -1 -stdin) {admin_username}"
    # )
    c.run(f"echo {admin_username}:{admin_password} | chpasswd")
    c.run(f"mkdir /home/{admin_username}/.ssh")
    c.run(f"echo {ssh_public_key} >> /home/{admin_username}/.ssh/authorized_keys")
    c.run(f"chsh -s /bin/bash {admin_username}")

    print("Setting up deploy account.")
    ## Setup deploy account
    c.run(f"useradd -m deploy")
    c.run(f"mkdir /home/deploy/.ssh")
    c.run(f"echo {ssh_deploy_public_key} >> /home/deploy/.ssh/authorized_keys")
    c.run(f"groupadd deployers")
    c.run(f"usermod -G deployers deploy")

    print("Setting up deployment directories.")
    ## Setup deployment
    c.run(f"mkdir /deployment")
    c.run(f"mkdir /deployment/scripts")
    c.run(f"mkdir /deployment/secrets")

    ### Populate with secrets
    print("Storing secrets.")
    c.put(io.StringIO(db_conn_string), "/deployment/secrets/db_conn_string")
    c.put(io.StringIO(db_conn_password), "/deployment/secrets/db_conn_password")
    c.put(io.StringIO(db_conn_user), "/deployment/secrets/db_conn_user")
    c.put(io.StringIO(db_conn_url), "/deployment/secrets/db_conn_url")
    c.put(io.StringIO(github_org_token), "/deployment/secrets/github_org_token")
    c.put(io.StringIO(github_org_user), "/deployment/secrets/github_org_user")
    c.put(io.StringIO(github_pck_token), "/deployment/secrets/github_pck_token")
    c.put(io.StringIO(github_pck_user), "/deployment/secrets/github_pck_user")
    c.put(io.StringIO(grafana_password), "/deployment/secrets/grafana_password")
    c.put(io.StringIO(grafana_user_email), "/deployment/secrets/grafana_user_email")
    c.put(
        io.StringIO(grafana_user_password), "/deployment/secrets/grafana_user_password"
    )
    c.put(
        io.StringIO(grafana_user_username), "/deployment/secrets/grafana_user_username"
    )
    c.put(logging_env_path, "/deployment/secrets/logging.env")

    print("Changing secret ownership.")
    c.run("chown :deployers /deployment/secrets/*")
    c.run("chmod o-r /deployment/secrets/*")

    print("Putting remote script")
    c.put(remote_script_path, "/deployment/scripts/remote.sh")
    c.run("chown :deployers /deployment/scripts/remote.sh")
    c.run("chmod g+x /deployment/scripts/remote.sh")
    c.run("usermod -s /deployment/scripts/remote.sh deploy")

    print("Adding deploy account to docker group")
    c.run("usermod -aG docker deploy")

    ## Harden SSH config
    ### Backup config
    print("Backing up sshd config")
    c.run("cp /etc/ssh/sshd_config /etc/ssh/sshd_config.factory-defaults")
    c.run("sudo chmod a-w /etc/ssh/sshd_config.factory-defaults")
    c.put(sshd_config_path, "/etc/ssh/sshd_config")

    print("Restarting VM.")
    try:
        c.run("shutdown -r now")
    except Exception:
        print("Called restart")

    print("VM restarted.")

    c.close()

    print("Waiting for 5 seconds to let the VM restart.")
    sleep(5)

    c = Connection(
        host=ip_address,
        user="deploy",
        connect_kwargs={"key_filename": ssh_deploy_private_key_path},
        connect_timeout=120,
    )

    print("Attempting to reconnect to VM to user deploy, to deploy the app.")
    done = False

    while not done:
        try:
            c.run("echo Hello!")
        except Exception:
            print("Could not connect yet. Trying again in 3 seconds")
            sleep(3)
        else:
            done = True
            print("Successfully reconnected to VM.")

    print("Closing connection.")
    c.close()
    print("Done! VM provisioned and app deployed.")


# From https://github.com/digitalocean/pydo/blob/main/examples/poc_droplets_volumes_sshkeys.py
# Accessed on the 11th of April, 2024
def wait_for_action(client, id, wait=5):
    print("Waiting for action {0} to complete...".format(id), end="", flush=True)
    status = "in-progress"
    while status == "in-progress":
        try:
            resp = client.actions.get(id)
        except Exception as err:
            raise err
        else:
            status = resp["action"]["status"]
            if status == "in-progress":
                print(".", end="", flush=True)
                sleep(wait)
            elif status == "errored":
                raise Exception(
                    "{0} action {1} {2}".format(
                        resp["action"]["type"], resp["action"]["id"], status
                    )
                )
            else:
                print(".")


# From https://github.com/digitalocean/pydo/blob/main/examples/poc_droplets_volumes_sshkeys.py
# Accessed on the 11th of April, 2024
def find_ssh_key(client, name):
    print("Looking for ssh key named {0}...".format(name))
    page = 1
    paginated = True
    while paginated:
        try:
            resp = client.ssh_keys.list(per_page=50, page=page)
            for k in resp["ssh_keys"]:
                if k["name"] == name:
                    print("Found ssh key: {0}".format(k["fingerprint"]))
                    return k
        except Exception as err:
            raise err

        pages = resp.links.pages
        if "next" in pages.keys():
            # Having to parse the URL to find the next page is not very friendly.
            parsed_url = urlparse(pages["next"])
            page = parse_qs(parsed_url.query)["page"][0]
        else:
            paginated = False

    raise Exception("no ssh key found")


def check_env_var(var, env_var):
    if var == "" or var == None:
        print_required_env_vars()
        raise Exception(f"No value for {env_var} env var")


def print_required_env_vars():
    print(
        """The following environment variables must be set before runing the script:
        DIGITALOCEAN_TOKEN
        DO_DB_NAME
        VM_NAME
        SSH_PUBLIC_KEY
        SSH_PRIVATE_KEY_PATH
        SSH_DEPLOY_PUBLIC_KEY
        SSH_DEPLOY_PRIVATE_KEY_PATH
        ADMIN_USERNAME
        ADMIN_PASSWORD
        DB_CONN_STRING
        DB_CONN_PASSWORD
        DB_CONN_URL
        DB_CONN_USER
        GITHUB_ORG_TOKEN
        GITHUB_ORG_USER
        GITHUB_PCK_TOKEN
        GITHUB_PCK_USER
        GRAFANA_PASSWORD
        GRAFANA_USER_EMAIL
        GRAFANA_USER_PASSWORD
        GRAFANA_USER_USERNAME
        LOGGING_ENV_PATH
        REMOTE_SCRIPT_PATH
        SSHD_CONFIG_PATH
    """
    )


if __name__ == "__main__":
    main()
