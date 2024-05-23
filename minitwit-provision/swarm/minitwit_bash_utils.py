import io
from time import sleep
from fabric import Connection, ThreadingGroup as Group


def wait_until_connected(connection):
    done = False

    while not done:
        try:
            connection.run("echo Hello!")
        except Exception:
            print("Could not connect yet. Trying again in 10 seconds")
            sleep(10)
        else:
            done = True
            print("Successfully connected to VM.")


def restart_node(connection):
    print("Attempting to restart VM.")
    try:
        connection.run(f"shutdown -r now")
    except Exception:
        print("Called restart")

    connection.close()

    print("Waiting for 5 seconds to let the VM restart.")
    sleep(5)


def restart_node_and_reconnect(
    connection: Connection, ip_address: str, ssh_private_key_path: str, username: str
) -> Connection:
    restart_node(connection)

    print("Attempting to reconnect to VM.")
    c = Connection(
        host=ip_address,
        user=f"{username}",
        connect_kwargs={"key_filename": ssh_private_key_path},
        connect_timeout=120,
    )

    wait_until_connected(c)

    return c


def update_packages(connection):
    print("Updating packages")
    connection.run(
        "while fuser /var/lib/apt/lists/lock >/dev/null 2>&1; do sleep 5; done;"
    )
    done = False
    while not done:
        try:
            connection.run("apt-get -o DPkg::Lock::Timeout=60 update -yq")
        except Exception:
            print("Error updating")
            print("Trying again in 10 seconds")
            sleep(10)
        else:
            done = True

    connection.run(
        "NEEDRESTART_MODE=a UCF_FORCE_CONFFOLD=1 DEBIAN_FRONTEND=noninteractive apt-get -o DPkg::Lock::Timeout=60 -o Dpkg::Options::=--force-confold -o Dpkg::Options::=--force-confdef --allow-downgrades --allow-remove-essential --allow-change-held-packages -yq dist-upgrade"
    )


def update_packages_with_new_connection(ip_address: str, ssh_private_key_path: str):
    c = Connection(
        host=ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    update_packages(c)


def install_docker(connection):
    connection.run(
        "for pkg in docker.io docker-doc docker-compose docker-compose-v2 podman-docker containerd runc; do apt-get remove $pkg -yq; done"
    )

    print("Begin installing docker")
    ### Add Docker's official GPG key:
    connection.run("apt-get update -yq")
    connection.run("apt-get install -yq ca-certificates curl")
    connection.run("install -m 0755 -d /etc/apt/keyrings")
    connection.run(
        "curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc"
    )
    connection.run("chmod a+r /etc/apt/keyrings/docker.asc")
    ### Add the repository to Apt sources:
    connection.run(
        'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null'
    )
    connection.run("apt-get update -yq")
    connection.run(
        "apt-get install -yq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin"
    )
    connection.run("sudo systemctl enable docker.service")
    connection.run("sudo systemctl enable containerd.service")

    print("Docker successfully installed")


def setup_admin_account(
    connection,
    admin_username: str,
    admin_password: str,
    ssh_public_key: str,
):
    print(f"Setting up admin account with username {admin_username}")
    ## Setup admin account
    connection.run(f"useradd -m {admin_username}")
    connection.run(f"adduser {admin_username} sudo")
    # connection.run(f"usermod --password {admin_password} {admin_username}")
    # connection.run(
    #     f"usermod --password $(echo {admin_password} | openssl passwd -1 -stdin) {admin_username}"
    # )
    connection.run(f"echo {admin_username}:{admin_password} | chpasswd")
    connection.run(f"mkdir /home/{admin_username}/.ssh")
    connection.run(
        f"echo {ssh_public_key} >> /home/{admin_username}/.ssh/authorized_keys"
    )
    connection.run(f"chsh -s /bin/bash {admin_username}")


def setup_docker_secrets(
    connection_to_manager,
    ssh_deploy_public_key,
    db_conn_string,
    db_conn_password,
    db_conn_user,
    db_conn_url,
    github_org_token,
    github_org_user,
    github_pck_token,
    github_pck_user,
    grafana_password,
    grafana_user_email,
    grafana_user_password,
    grafana_user_username,
    logging_env_path,
    remote_script_path,
):
    secrets_dict = {
        "db_conn_string": db_conn_string,
        "db_conn_password": db_conn_password,
        "db_conn_url": db_conn_url,
        "grafana_password": grafana_password,
        "grafana_user_email": grafana_user_email,
        "grafana_user_password": grafana_user_password,
        "grafana_user_username": grafana_user_username,
    }

    add_multiple_docker_secrets(connection_to_manager, secrets_dict)


def add_multiple_docker_secrets(connection, secret_name_value_pairs: dict[str, str]):
    for name, value in secret_name_value_pairs.items():
        add_single_docker_secret(connection, name, value)


def add_single_docker_secret(connection, secret_name: str, secret_value: str):
    connection.run(f"printf {secret_value} | docker secret create {secret_name} -")


def give_user_docker_access(connection, username: str):
    print(f"Adding {username} account to docker group")
    connection.run(f"usermod -aG docker {username}")


def create_bind_dirs(connection):
    connection.run("mkdir /prometheus")


def setup_deploy_account(
    connection,
    ssh_deploy_public_key: str,
    db_conn_string: str,
    db_conn_password: str,
    db_conn_user: str,
    db_conn_url: str,
    github_org_token: str,
    github_org_user: str,
    github_pck_token: str,
    github_pck_user: str,
    grafana_password: str,
    grafana_user_email: str,
    grafana_user_password: str,
    grafana_user_username: str,
    logging_env_path: str,
    remote_script_path: str,
):
    print("Setting up deploy account.")
    ## Setup deploy account
    connection.run(f"useradd -m deploy")
    connection.run(f"mkdir /home/deploy/.ssh")
    connection.run(f"echo {ssh_deploy_public_key} >> /home/deploy/.ssh/authorized_keys")
    connection.run(f"groupadd deployers")
    connection.run(f"usermod -G deployers deploy")

    print("Setting up deployment directories.")
    ## Setup deployment
    connection.run(f"mkdir /deployment")
    connection.run(f"mkdir /deployment/scripts")
    connection.run(f"mkdir /deployment/secrets")

    ### Populate with secrets
    print("Storing secrets.")
    connection.put(io.StringIO(db_conn_string), "/deployment/secrets/db_conn_string")
    connection.put(
        io.StringIO(db_conn_password), "/deployment/secrets/db_conn_password"
    )
    connection.put(io.StringIO(db_conn_user), "/deployment/secrets/db_conn_user")
    connection.put(io.StringIO(db_conn_url), "/deployment/secrets/db_conn_url")
    connection.put(
        io.StringIO(github_org_token), "/deployment/secrets/github_org_token"
    )
    connection.put(io.StringIO(github_org_user), "/deployment/secrets/github_org_user")
    connection.put(
        io.StringIO(github_pck_token), "/deployment/secrets/github_pck_token"
    )
    connection.put(io.StringIO(github_pck_user), "/deployment/secrets/github_pck_user")
    connection.put(
        io.StringIO(grafana_password), "/deployment/secrets/grafana_password"
    )
    connection.put(
        io.StringIO(grafana_user_email), "/deployment/secrets/grafana_user_email"
    )
    connection.put(
        io.StringIO(grafana_user_password), "/deployment/secrets/grafana_user_password"
    )
    connection.put(
        io.StringIO(grafana_user_username), "/deployment/secrets/grafana_user_username"
    )
    connection.put(logging_env_path, "/deployment/secrets/logging.env")

    print("Changing secret ownership.")
    connection.run("chown :deployers /deployment/secrets/*")
    connection.run("chmod o-r /deployment/secrets/*")

    print("Putting remote script")
    connection.put(remote_script_path, "/deployment/scripts/remote.sh")
    connection.run("chown :deployers /deployment/scripts/remote.sh")
    connection.run("chmod g+x /deployment/scripts/remote.sh")
    connection.run("usermod -s /deployment/scripts/remote.sh deploy")

    give_user_docker_access(connection, "deploy")


def harden_ssh_config(connection, sshd_config_path: str):
    ## Harden SSH config
    ### Backup config
    print("Backing up sshd config")
    connection.run("cp /etc/ssh/sshd_config /etc/ssh/sshd_config.factory-defaults")
    connection.run("sudo chmod a-w /etc/ssh/sshd_config.factory-defaults")
    connection.put(sshd_config_path, "/etc/ssh/sshd_config")


def provision_nodes(
    public_ip_addresses,
    leader_ip_address,
    ssh_private_key_path,
    ssh_public_key,
    ssh_deploy_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
    db_conn_string,
    db_conn_password,
    db_conn_user,
    db_conn_url,
    github_org_token,
    github_org_user,
    github_pck_token,
    github_pck_user,
    grafana_password,
    grafana_user_email,
    grafana_user_password,
    grafana_user_username,
    logging_env_path,
    remote_script_path,
):
    print(public_ip_addresses)

    nodes = Group(
        *public_ip_addresses,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    print("Waiting 15 seconds")
    sleep(15)

    wait_until_connected(nodes)

    update_packages(nodes)

    restart_node(nodes)

    print("Attempting to reconnect to VM's.")
    nodes = Group(
        *public_ip_addresses,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
        connect_timeout=120,
    )

    wait_until_connected(nodes)

    install_docker(nodes)

    open_docker_ports(nodes)

    setup_admin_account(nodes, admin_username, admin_password, ssh_public_key)

    give_user_docker_access(nodes, admin_username)

    leader_connection = Connection(
        host=leader_ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    setup_deploy_account(
        leader_connection,
        ssh_deploy_public_key,
        db_conn_string,
        db_conn_password,
        db_conn_user,
        db_conn_url,
        github_org_token,
        github_org_user,
        github_pck_token,
        github_pck_user,
        grafana_password,
        grafana_user_email,
        grafana_user_password,
        grafana_user_username,
        logging_env_path,
        remote_script_path,
    )

    harden_ssh_config(nodes, sshd_config_path)

    restart_node(nodes)

    print("Checking that we can reconnect")

    nodes = Group(
        *public_ip_addresses,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
        connect_timeout=120,
    )

    wait_until_connected(nodes)

    print("Closing connections.")
    nodes.close()

    print("Done! VM's provisioned.")


def provision_leader(
    public_ip_address,
    ssh_private_key_path,
    ssh_public_key,
    ssh_deploy_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
    db_conn_string,
    db_conn_password,
    db_conn_user,
    db_conn_url,
    github_org_token,
    github_org_user,
    github_pck_token,
    github_pck_user,
    grafana_password,
    grafana_user_email,
    grafana_user_password,
    grafana_user_username,
    logging_env_path,
    remote_script_path,
):
    # Connect to VM and run provisioning
    # ip_address = None

    print("Connecting to VM to begin provisioning.")

    c = Connection(
        host=public_ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    wait_until_connected(c)

    ## Update packages
    update_packages(c)

    c = restart_node_and_reconnect(c, public_ip_address, ssh_private_key_path, "root")

    ## Install Docker
    install_docker(c)

    open_docker_ports(c)

    setup_admin_account(c, admin_username, admin_password, ssh_public_key)

    give_user_docker_access(c, admin_username)

    setup_deploy_account(
        c,
        ssh_deploy_public_key,
        db_conn_string,
        db_conn_password,
        db_conn_user,
        db_conn_url,
        github_org_token,
        github_org_user,
        github_pck_token,
        github_pck_user,
        grafana_password,
        grafana_user_email,
        grafana_user_password,
        grafana_user_username,
        logging_env_path,
        remote_script_path,
    )

    harden_ssh_config(c, sshd_config_path)

    c = restart_node_and_reconnect(
        c, public_ip_address, ssh_private_key_path, admin_username
    )

    # init_docker_swarm(c)

    print("Closing connection.")
    c.close()
    print("Done! VM provisioned and app deployed.")


def provision_manager(
    public_ip_address,
    ssh_private_key_path,
    ssh_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
):
    print("Connecting to VM to begin provisioning.")

    c = Connection(
        host=public_ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    wait_until_connected(c)

    ## Update packages
    update_packages(c)

    c = restart_node_and_reconnect(c, public_ip_address, ssh_private_key_path, "root")

    ## Install Docker
    install_docker(c)

    open_docker_ports(c)

    setup_admin_account(c, admin_username, admin_password, ssh_public_key)

    give_user_docker_access(c, admin_username)

    harden_ssh_config(c, sshd_config_path)

    c = restart_node_and_reconnect(
        c, public_ip_address, ssh_private_key_path, admin_username
    )

    print("Closing connection.")
    c.close()
    print("Done! VM provisioned and app deployed.")


def provision_worker(
    public_ip_address,
    ssh_private_key_path,
    ssh_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
):
    print("Connecting to VM to begin provisioning.")

    c = Connection(
        host=public_ip_address,
        user="root",
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    wait_until_connected(c)

    ## Update packages
    update_packages(c)

    c = restart_node_and_reconnect(c, public_ip_address, ssh_private_key_path, "root")

    ## Install Docker
    install_docker(c)

    open_docker_ports(c)

    setup_admin_account(c, admin_username, admin_password, ssh_public_key)

    give_user_docker_access(c, admin_username)

    harden_ssh_config(c, sshd_config_path)

    c = restart_node_and_reconnect(
        c, public_ip_address, ssh_private_key_path, admin_username
    )

    print("Closing connection.")
    c.close()
    print("Done! VM provisioned and app deployed.")


def deploy_on_manager(manager_ip_address, ssh_deploy_private_key_path):
    deploy_connection = Connection(
        host=manager_ip_address,
        user="deploy",
        connect_kwargs={"key_filename": ssh_deploy_private_key_path},
    )

    deploy_connection.run("echo deploy")
    deploy_connection.close()


def open_docker_ports(connection):
    print("Opening docker ports")
    try:
        connection.run("ufw allow 22/tcp")
        connection.run("ufw allow 2376/tcp")
        connection.run("ufw allow 2377/tcp")
        connection.run("ufw allow 7946/tcp")
        connection.run("ufw allow 7946/udp")
        connection.run("ufw allow 4789/udp")
        connection.run("ufw reload")
        connection.run("ufw --force  enable")
        connection.run("systemctl restart docker")
    except Exception as err:
        raise Exception("Error opening docker ports:", err)
    else:
        print("Finished opening docker ports")


def init_docker_swarm(connection_to_manager: Connection, ip_address_to_advertise: str):
    print("Initialising docker swarm")
    try:
        connection_to_manager.run(
            f"docker swarm init --advertise-addr {ip_address_to_advertise}"
        )
    except Exception as err:
        raise Exception("Error initialising docker swarm:", err)
    else:
        print("Finished initialising docker swarm")


def init_docker_swarm_from_ip(
    initiator_public_ip_address,
    initiator_private_ip_address,
    admin_username,
    ssh_private_key_path,
):
    initiator_connection = Connection(
        host=initiator_public_ip_address,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
    )
    init_docker_swarm(initiator_connection, initiator_private_ip_address)


def fetch_worker_join_token_from_manager(connection_to_manager: Connection) -> str:
    try:
        result = connection_to_manager.run("docker swarm join-token worker -q")
    except Exception as err:
        raise Exception("Error fetching join token from the manager:", err)
    else:
        return result.stdout.strip()


def fetch_manager_join_token_from_manager(connection_to_manager: Connection) -> str:
    try:
        result = connection_to_manager.run("docker swarm join-token manager -q")
    except Exception as err:
        raise Exception("Error fetching join token from the manager:", err)
    else:
        return result.stdout


def join_node_to_manager(
    connection_to_joiner: Connection, join_token, manager_private_ip_address
):
    try:
        connection_to_joiner.run(
            f"docker swarm join --token {join_token} {manager_private_ip_address}:2377"
        )
    except Exception as err:
        raise Exception(f"Error joining nodes:", err)


def join_worker_to_manager(
    connection_to_worker: Connection,
    connection_to_manager: Connection,
    manager_private_ip_address,
):
    join_token = fetch_worker_join_token_from_manager(connection_to_manager)
    join_node_to_manager(connection_to_worker, join_token, manager_private_ip_address)


def join_manager_to_manager(
    connection_to_joining_manager: Connection,
    connection_to_joined_manager: Connection,
    joined_manager_private_ip_address: str,
):
    join_token = fetch_manager_join_token_from_manager(connection_to_joined_manager)
    join_node_to_manager(
        connection_to_joining_manager, join_token, joined_manager_private_ip_address
    )


def join_manager_to_manager_with_addresses(
    joining_manager_public_ip_address,
    joined_manager_public_ip_address,
    joined_manager_private_ip_address,
    admin_username,
    ssh_private_key_path,
):
    joining_manager_connection = Connection(
        host=joining_manager_public_ip_address,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    joined_manager_connection = Connection(
        host=joined_manager_public_ip_address,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    join_manager_to_manager(
        joining_manager_connection,
        joined_manager_connection,
        joined_manager_private_ip_address,
    )


def join_worker_to_manager_with_addresses(
    worker_public_ip_address,
    manager_public_ip_address,
    manager_private_ip_address,
    admin_username,
    ssh_private_key_path,
):
    worker_connection = Connection(
        host=worker_public_ip_address,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    mananger_connection = Connection(
        host=manager_public_ip_address,
        user=admin_username,
        connect_kwargs={"key_filename": ssh_private_key_path},
    )

    join_worker_to_manager(
        worker_connection, mananger_connection, manager_private_ip_address
    )
