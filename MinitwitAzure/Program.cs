/**
 * Generate a new minitwit VM
 * Based on https://github.com/Azure-Samples/azure-samples-net-management
 * (Accessed on the 21st of February, 2024)
*/
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources; // See https://aka.ms/new-console-template for more information

namespace MiniTwitAzure
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string AdminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
            string AdminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            string ComputerName = Environment.GetEnvironmentVariable("COMPUTER_NAME");
            string SshPublicKey = Environment.GetEnvironmentVariable("SSH_PUBLIC_KEY");
            string VirtualNetworkName = Environment.GetEnvironmentVariable("VNET_NAME");
            string SubnetName = Environment.GetEnvironmentVariable("SUBNET_NAME");
            string IpConfigName = Environment.GetEnvironmentVariable("IP_CONFIG_NAME");
            string NetworkInterfaceName = Environment.GetEnvironmentVariable(
                "NETWORK_INTERFACE_NAME"
            );
            string OsDiskName = Environment.GetEnvironmentVariable("OS_DISK_NAME");
            string VmName = Environment.GetEnvironmentVariable("VM_NAME");
            string ResourceGroupName = Environment.GetEnvironmentVariable("RESOURCE_GROUP_NAME");

            string[] neccesaryArgs =
            [
                AdminUsername,
                AdminPassword,
                ComputerName,
                SshPublicKey,
                VirtualNetworkName,
                SubnetName,
                IpConfigName,
                NetworkInterfaceName,
                OsDiskName,
                VmName,
                ResourceGroupName
            ];

            foreach (string arg in neccesaryArgs)
            {
                if (arg == null || arg == "")
                {
                    Console.WriteLine("One or more environment variables was not set.");
                    Console.WriteLine("The required env variables are:");
                    Console.WriteLine("ADMIN_USERNAME");
                    Console.WriteLine("ADMIN_PASSWORD");
                    Console.WriteLine("COMPUTER_NAME");
                    Console.WriteLine("SSH_PUBLIC_KEY");
                    Console.WriteLine("VNET_NAME");
                    Console.WriteLine("SUBNET_NAME");
                    Console.WriteLine("IP_CONFIG_NAME");
                    Console.WriteLine("NETWORK_INTERFACE_NAME");
                    Console.WriteLine("OS_DISK_NAME");
                    Console.WriteLine("VM_NAME");
                    Console.WriteLine("RESOURCE_GROUP_NAME");

                    return 1;
                }
            }

            TokenCredential cred;
            ArmClient client;

            try
            {
                // From https://learn.microsoft.com/en-us/rest/api/compute/virtual-machines/create-or-update?view=rest-compute-2023-10-02&tabs=dotnet#create-a-vm-from-a-specialized-shared-image.
                // Accessed on the 21st of February, 2024
                // get your azure access token, for more details of how Azure SDK get your access token, please refer to https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication?tabs=command-line
                cred = new DefaultAzureCredential();
                // authenticate your client
                client = new ArmClient(cred);
                // See https://aka.ms/new-console-template for more information
            }
            catch (Exception e)
            {
                Console.WriteLine("Error authenticating your Azure account.");
                Console.WriteLine("Make sure you're logged in on this machine.");
                Console.WriteLine("Error message: " + e.Message);

                return 1;
            }

            if (cred == null || client == null)
            {
                Console.WriteLine("Error authenticating your Azure account.");
                Console.WriteLine("Make sure you're logged in on this machine.");

                return 1;
            }

            string location = AzureLocation.NorthEurope;

            ResourceGroupResource resourceGroup = (
                await client
                    .GetDefaultSubscription()
                    .GetResourceGroups()
                    .CreateOrUpdateAsync(
                        Azure.WaitUntil.Completed,
                        ResourceGroupName,
                        new ResourceGroupData(location)
                    )
            ).Value;

            var collection = resourceGroup.GetVirtualMachines();

            // Create VNet
            Console.WriteLine("--------Start create VNet--------");
            var vnetData = new VirtualNetworkData()
            {
                Location = location,
                AddressPrefixes = { "10.0.0.0/16" },
                Subnets =
                {
                    new SubnetData() { Name = SubnetName, AddressPrefix = "10.0.0.0/28" }
                }
            };

            var vnetCollection = resourceGroup.GetVirtualNetworks();

            var vnet = (
                await vnetCollection.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    VirtualNetworkName,
                    vnetData
                )
            ).Value;

            var nicData = new NetworkInterfaceData()
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = IpConfigName,
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        Primary = false,
                        Subnet = new SubnetData() { Id = vnet.GetSubnet(SubnetName).Value.Id }
                    }
                }
            };

            var nicCollection = resourceGroup.GetNetworkInterfaces();
            var nic = (
                await nicCollection.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    NetworkInterfaceName,
                    nicData
                )
            ).Value;

            // Create VM
            Console.WriteLine("--------Start create VM--------");
            var vmData = new VirtualMachineData(location)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardB1S
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    AdminUsername = AdminUsername,
                    AdminPassword = AdminPassword,
                    ComputerName = ComputerName,
                    LinuxConfiguration = new LinuxConfiguration()
                    {
                        DisablePasswordAuthentication = true,
                        SshPublicKeys =
                        {
                            new SshPublicKeyConfiguration()
                            {
                                Path = $"/home/{AdminUsername}/.ssh/authorized_keys",
                                KeyData = SshPublicKey,
                            }
                        }
                    }
                },
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = nic.Id,
                            Primary = true,
                        }
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                    {
                        Name = OsDiskName,
                        OSType = SupportedOperatingSystemType.Linux,
                        Caching = CachingType.ReadWrite,
                        ManagedDisk = new VirtualMachineManagedDisk()
                        {
                            StorageAccountType = StorageAccountType.StandardLrs
                        }
                    },
                    ImageReference = new ImageReference()
                    {
                        Publisher = "Canonical",
                        Offer = "0001-com-ubuntu-server-jammy",
                        Sku = "22_04-lts-gen2",
                        Version = "latest",
                    },
                }
            };

            var resource = await collection.CreateOrUpdateAsync(
                Azure.WaitUntil.Completed,
                VmName,
                vmData
            );
            Console.WriteLine("VM ID: " + resource.Value.Id);
            Console.WriteLine("--------Done create VM--------");

            return 0;
        }
    }
}
