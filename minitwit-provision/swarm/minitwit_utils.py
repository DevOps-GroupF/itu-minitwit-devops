import minitwit_bash_utils
import minitwit_do_utils


def pre_flight_checks(digital_ocean_token, db_name, node_name_prefix):
    minitwit_do_utils.do_checks(digital_ocean_token, db_name, node_name_prefix)


def create_manager_nodes(
    num_nodes,
    do_token,
    db_name,
    name_prefix,
    ssh_key_name,
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
) -> list[tuple[str, str]]:
    pre_flight_checks(do_token, db_name, name_prefix)

    ip_addresses = []

    leader_ip_address_pair = create_leader_node(
        do_token,
        db_name,
        f"{name_prefix}-leader",
        ssh_key_name,
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
    )

    ip_addresses.append(leader_ip_address_pair)

    for i in range(0, num_nodes - 1):
        manager_ip_address_pair = create_single_manager_node(
            do_token,
            db_name,
            f"{name_prefix}-manager-{i}",
            ssh_key_name,
            ssh_private_key_path,
            ssh_public_key,
            admin_username,
            admin_password,
            sshd_config_path,
        )

        ip_addresses.append(manager_ip_address_pair)

    return ip_addresses


def create_single_manager_node(
    do_token,
    db_name,
    name,
    ssh_key_name,
    ssh_private_key_path,
    ssh_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
) -> tuple[str, str]:
    (
        public_ip_address,
        private_ip_address,
    ) = minitwit_do_utils.create_droplet_and_allow_on_firewall(
        do_token, name, ssh_key_name, ssh_public_key, db_name
    )

    minitwit_bash_utils.provision_manager(
        public_ip_address,
        ssh_private_key_path,
        ssh_public_key,
        admin_username,
        admin_password,
        sshd_config_path,
    )

    return (public_ip_address, private_ip_address)


def create_leader_node(
    do_token,
    db_name,
    name,
    ssh_key_name,
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
) -> tuple[str, str]:
    # Create Droplet

    (
        public_ip_address,
        private_ip_address,
    ) = minitwit_do_utils.create_droplet_and_allow_on_firewall(
        do_token, name, ssh_key_name, ssh_public_key, db_name
    )

    minitwit_bash_utils.provision_leader(
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
    )

    return (public_ip_address, private_ip_address)


def create_worker_nodes(
    num_nodes,
    do_token,
    db_name,
    name_prefix,
    ssh_key_name,
    ssh_private_key_path,
    ssh_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
) -> list[tuple[str, str]]:
    ip_addresses = []

    for i in range(0, num_nodes):
        worker_ip_address_pair = create_single_worker_node(
            do_token,
            db_name,
            f"{name_prefix}-worker-{i}",
            ssh_key_name,
            ssh_private_key_path,
            ssh_public_key,
            admin_username,
            admin_password,
            sshd_config_path,
        )

        ip_addresses.append(worker_ip_address_pair)

    return ip_addresses


def create_single_worker_node(
    do_token,
    db_name,
    name,
    ssh_key_name,
    ssh_private_key_path,
    ssh_public_key,
    admin_username,
    admin_password,
    sshd_config_path,
) -> tuple[str, str]:
    (
        public_ip_address,
        private_ip_address,
    ) = minitwit_do_utils.create_droplet_and_allow_on_firewall(
        do_token, name, ssh_key_name, ssh_public_key, db_name
    )

    minitwit_bash_utils.provision_worker(
        public_ip_address,
        ssh_private_key_path,
        ssh_public_key,
        admin_username,
        admin_password,
        sshd_config_path,
    )

    return (public_ip_address, private_ip_address)


def init_swarm(
    initiator_public_ip_address,
    initiator_private_ip_address,
    admin_username,
    ssh_private_key_path,
):
    minitwit_bash_utils.init_docker_swarm_from_ip(
        initiator_public_ip_address,
        initiator_private_ip_address,
        admin_username,
        ssh_private_key_path,
    )


def spawn_new_cluster(
    num_managers,
    num_workers,
    env_token,
    do_db_name,
    node_name_prefix,
    ssh_key_name,
    ssh_private_key_path,
    ssh_public_key,
    ssh_deploy_public_key,
    ssh_deploy_private_key_path,
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
    manager_ip_addresses, worker_ip_addresses = minitwit_do_utils.create_droplets(
        do_token=env_token,
        node_name_prefix=node_name_prefix,
        db_name=do_db_name,
        ssh_key_name=ssh_key_name,
        ssh_public_key=ssh_public_key,
        num_managers=num_managers,
        num_workers=num_workers,
    )

    all_ip_addresses = manager_ip_addresses + worker_ip_addresses
    all_public_ip_addresses = list(map(lambda t: t[0], all_ip_addresses))

    leader_ip_address_pair = manager_ip_addresses[0]

    minitwit_bash_utils.provision_nodes(
        public_ip_addresses=all_public_ip_addresses,
        leader_ip_address=leader_ip_address_pair[0],
        ssh_private_key_path=ssh_private_key_path,
        ssh_public_key=ssh_public_key,
        ssh_deploy_public_key=ssh_deploy_public_key,
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

    init_swarm(
        leader_ip_address_pair[0],
        leader_ip_address_pair[1],
        admin_username,
        ssh_private_key_path,
    )

    bind_nodes(
        leader_ip_address_pair[0],
        leader_ip_address_pair[1],
        manager_ip_addresses,
        worker_ip_addresses,
        admin_username,
        ssh_private_key_path,
    )

    print("Deploying stack")
    minitwit_bash_utils.deploy_on_manager(
        leader_ip_address_pair[0], ssh_deploy_private_key_path
    )


def bind_nodes(
    leader_public_ip_address: str,
    leader_private_ip_address: str,
    manager_ip_addresses: list[tuple[str, str]],
    worker_ip_addresses: list[tuple[str, str]],
    admin_username: str,
    ssh_private_key_path: str,
):
    for i in range(1, len(manager_ip_addresses)):
        manager_public_ip_address = manager_ip_addresses[i][0]
        minitwit_bash_utils.join_manager_to_manager_with_addresses(
            manager_public_ip_address,
            leader_public_ip_address,
            leader_private_ip_address,
            admin_username,
            ssh_private_key_path,
        )

    for i in range(0, len(worker_ip_addresses)):
        worker_public_ip_address = worker_ip_addresses[i][0]
        minitwit_bash_utils.join_worker_to_manager_with_addresses(
            worker_public_ip_address,
            leader_public_ip_address,
            leader_private_ip_address,
            admin_username,
            ssh_private_key_path,
        )


def init_bind_deploy(
    leader_public_ip_address,
    leader_private_ip_address,
    admin_username,
    ssh_private_key_path,
    manager_ip_addresses,
    worker_ip_addresses,
    ssh_deploy_private_key_path,
):
    init_swarm(
        leader_public_ip_address,
        leader_private_ip_address,
        admin_username,
        ssh_private_key_path,
    )

    bind_nodes(
        leader_public_ip_address,
        leader_private_ip_address,
        manager_ip_addresses,
        worker_ip_addresses,
        admin_username,
        ssh_private_key_path,
    )

    print("Deploying stack")
    minitwit_bash_utils.deploy_on_manager(
        leader_public_ip_address, ssh_deploy_private_key_path
    )
