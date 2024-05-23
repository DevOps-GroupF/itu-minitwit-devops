import os
from dotenv import load_dotenv
import argparse

import minitwit_utils

parser = argparse.ArgumentParser()
parser.add_argument("--skip_do", action="store_true", help="Skip creating a vm")

args = parser.parse_args()

NUM_MANAGERS = 1

NUM_WORKERS = 3


def main():
    load_dotenv()

    env_token = os.environ.get("DIGITALOCEAN_TOKEN")
    check_env_var(env_token, "DIGITALOCEAN_TOKEN")

    do_db_name = os.environ.get("DO_DB_NAME")
    check_env_var(do_db_name, "DO_DB_NAME")

    node_name_prefix = os.environ.get("NODE_NAME_PREFIX")
    check_env_var(node_name_prefix, "NODE_NAME_PREFIX")

    ssh_key_name = os.environ.get("SSH_KEY_NAME")

    if ssh_key_name == "" or ssh_key_name == None:
        ssh_key_name = f"ssh-key-{node_name_prefix}"
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
        minitwit_utils.pre_flight_checks(env_token, do_db_name, node_name_prefix)

        minitwit_utils.spawn_new_cluster(
            num_managers=NUM_MANAGERS,
            num_workers=NUM_WORKERS,
            env_token=env_token,
            do_db_name=do_db_name,
            node_name_prefix=node_name_prefix,
            ssh_key_name=ssh_key_name,
            ssh_private_key_path=ssh_private_key_path,
            ssh_public_key=ssh_public_key,
            ssh_deploy_public_key=ssh_deploy_public_key,
            ssh_deploy_private_key_path=ssh_deploy_private_key_path,
            admin_username=admin_username,
            admin_password=admin_password,
            sshd_config_path=sshd_config_path,
            db_conn_string=db_conn_string,
            db_conn_password=db_conn_password,
            db_conn_user=db_conn_user,
            db_conn_url=db_conn_url,
            github_org_token=github_org_token,
            github_org_user=github_org_user,
            github_pck_token=github_pck_token,
            github_pck_user=github_pck_user,
            grafana_password=grafana_password,
            grafana_user_email=grafana_user_email,
            grafana_user_password=grafana_user_password,
            grafana_user_username=grafana_user_username,
            logging_env_path=logging_env_path,
            remote_script_path=remote_script_path,
        )


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
