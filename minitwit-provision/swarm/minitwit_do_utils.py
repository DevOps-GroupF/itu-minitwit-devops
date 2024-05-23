from time import sleep
from urllib.parse import urlparse, parse_qs
from pydo import Client

REGION = "fra1"
SIZE = "s-1vcpu-1gb"
IMAGE = "ubuntu-23-10-x64"


def check_account(client):
    account = None

    try:
        print("Trying to fetch DO account")
        account = client.account.get()
    except Exception as error:
        print(f"Error getting account from DO: {error}")
        raise Exception("Error connecting to DO")
    else:
        print("Successfully fetched account info for ", account["account"]["name"])


def fetch_db_id_from_db_name(client, db_name):
    db_response = client.databases.list_clusters()
    databases = db_response["databases"]

    if len(databases) == 0:
        return None

    db_found_id = None

    for db in databases:
        if db_found_id is None and db["name"] == db_name:
            if not db["status"] == "online":
                raise Exception(
                    f"A DB with the name {db_name} was found, but it is not running."
                )
            db_found_id = db["id"]

    return db_found_id


def check_db(client, db_name):
    print(f"Checking if a database with the name {db_name} exists.")

    found_db_id = fetch_db_id_from_db_name(client, db_name)

    if found_db_id is None:
        raise Exception(f"Did not find a DB with the name {db_name}.")

    print(f"Found a DB with the name {db_name}. Proceeding with provisioning.")


def check_node_name_prefix(client, node_name_prefix):
    print(f"Checking if the node name prefix {node_name_prefix} is already in use")
    for droplet in client.droplets.list()["droplets"]:
        if droplet["name"].startswith(node_name_prefix):
            raise Exception("VM name already taken")

    print(f"VM name {node_name_prefix} is not in use")


def find_or_create_ssh_key(client, ssh_key_name, ssh_public_key):
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

    return key_fingerprint


def do_checks(digital_ocean_token, db_name, node_name_prefix):
    client = Client(token=digital_ocean_token)

    check_account(client)
    check_db(client, db_name)
    check_node_name_prefix(client, node_name_prefix)


def fetch_ip_addresses_from_droplet(droplet) -> tuple[str, str]:
    public_ip_address = ""
    private_ip_address = ""

    for net in droplet["networks"]["v4"]:
        if net["type"] == "public":
            public_ip_address = str(net["ip_address"])
        elif net["type"] == "private":
            private_ip_address = str(net["ip_address"])

    if public_ip_address == "" or private_ip_address == "":
        raise Exception(
            "Could not find both a public and a private address for the droplet"
        )

    return (public_ip_address, private_ip_address)


def create_droplet(client: Client, name, ssh_key_name, ssh_public_key):
    key_fingerprint = find_or_create_ssh_key(client, ssh_key_name, ssh_public_key)

    droplet_creation_request_body = {
        "name": name,
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

    return created_droplet


def allow_droplet_on_db_firewall(client, droplet_id, db_id):
    print(
        f'Getting all the firewall rules so the Droplet with id "{droplet_id}" can be added to the allow-list.'
    )

    db_rules = client.databases.list_firewall_rules(database_cluster_uuid=db_id)

    new_rule = {"type": "droplet", "value": f"{droplet_id}"}
    db_rules["rules"].append(new_rule)

    print(db_rules)

    print(
        f'Adding the Droplet with id "{droplet_id}" to the allow-list of DB with id "{db_id}".'
    )

    client.databases.update_firewall_rules(database_cluster_uuid=db_id, body=db_rules)

    print(
        f'Added the Droplet with id "{droplet_id}" to the allow-list of DB with id "{db_id}".'
    )


def create_droplet_and_allow_on_firewall(
    do_token, name, ssh_key_name, ssh_public_key, db_name
):
    client = Client(token=do_token)

    created_droplet = create_droplet(client, name, ssh_key_name, ssh_public_key)

    db_id = fetch_db_id_from_db_name(client, db_name)

    allow_droplet_on_db_firewall(client, created_droplet["id"], db_id)

    return fetch_ip_addresses_from_droplet(created_droplet)


def create_droplets(
    do_token: str,
    node_name_prefix: str,
    num_managers: int,
    num_workers: int,
    ssh_key_name: str,
    ssh_public_key: str,
    db_name: str,
) -> tuple[list[tuple[str, str]], list[tuple[str, str]]]:
    if (num_managers + num_workers) > 10:
        raise Exception("Can't create more than 10 droplets at a time")

    # Create names
    manager_node_names = [
        f"{node_name_prefix}-manager-{i}" for i in range(num_managers)
    ]
    worker_node_names = [f"{node_name_prefix}-worker-{i}" for i in range(num_workers)]

    client = Client(token=do_token)

    key_fingerprint = find_or_create_ssh_key(client, ssh_key_name, ssh_public_key)

    droplet_creation_request_body = {
        "names": manager_node_names + worker_node_names,
        "region": REGION,
        "size": SIZE,
        "image": IMAGE,
        "ssh_keys": [key_fingerprint],
    }

    print("Creating Droplets using: {0}".format(droplet_creation_request_body))

    response = client.droplets.create(body=droplet_creation_request_body)

    droplet_ids = list(map(lambda droplet: droplet["id"], response["droplets"]))

    print("VM creation request sent.")

    droplet_creation_action_ids = list(
        map(lambda action: action["id"], response["links"]["actions"])
    )

    for action_id in droplet_creation_action_ids:
        wait_for_action(client, action_id)

    created_droplets = [
        client.droplets.get(droplet_id=id)["droplet"] for id in droplet_ids
    ]

    manager_ip_addresses = []
    worker_ip_addresses = []

    db_id = fetch_db_id_from_db_name(client, db_name)

    for droplet in created_droplets:
        allow_droplet_on_db_firewall(client, droplet["id"], db_id)

        public_ip_address = ""
        private_ip_address = ""

        for net in droplet["networks"]["v4"]:
            if net["type"] == "public":
                public_ip_address = net["ip_address"]
            elif net["type"] == "private":
                private_ip_address = net["ip_address"]

        ip_address_pair = (public_ip_address, private_ip_address)

        if droplet["name"].startswith(f"{node_name_prefix}-manager"):
            manager_ip_addresses.append(ip_address_pair)
        elif droplet["name"].startswith(f"{node_name_prefix}-worker"):
            worker_ip_addresses.append(ip_address_pair)

    return (manager_ip_addresses, worker_ip_addresses)


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
