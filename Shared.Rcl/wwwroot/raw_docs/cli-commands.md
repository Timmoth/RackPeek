# CLI Commands

## `rpk`
```
USAGE:
    rpk [OPTIONS] <COMMAND>

EXAMPLES:
    rpk discover system
    rpk discover system --name nas01 --push
    rpk discover docker
    rpk discover docker --push
    rpk discover proxmox --host https://pve.lan:8006 --insecure

OPTIONS:
    -h, --help       Prints help information   
    -v, --version    Prints version information

COMMANDS:
    summary         Show a summarized report of all resources in the system    
    servers         Manage servers and their components                        
    switches        Manage network switches                                    
    routers         Manage network routers                                     
    firewalls       Manage firewalls                                           
    systems         Manage systems and their dependencies                      
    accesspoints    Manage access points                                       
    ups             Manage UPS units                                           
    other           Manage other hardware that doesn't fit an existing category
    desktops        Manage desktop computers and their components              
    laptops         Manage Laptop computers and their components               
    services        Manage services and their configurations                   
    discover        Read infrastructure and emit it as RackPeek YAML           
    ansible         Generate and manage Ansible inventory                      
    ssh             Generate SSH configuration from infrastructure             
    hosts           Generate a hosts file from infrastructure                  
    graph           Render inventory as graph diagrams                         
    tags            Discover tags across resources                             
    connections     Manage physical or logical port connections                
```

## `rpk summary`
```
DESCRIPTION:
Show a summarized report of all resources in the system

USAGE:
    rpk summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers`
```
DESCRIPTION:
Manage servers and their components

USAGE:
    rpk servers [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a summarized hardware report for all       
                                servers                                         
    add <name>                  Add a new server to the inventory               
    get <name>                  List all servers or retrieve a specific server  
                                by name                                         
    describe <name>             Display detailed information about a specific   
                                server                                          
    set <name>                  Update properties of an existing server         
    del <name>                  Delete a server from the inventory              
    rename <name> <new-name>    Rename a server to a new name                   
    tree <name>                 Display the dependency tree of a server         
    cpu                         Manage CPUs attached to a server                
    drive                       Manage drives attached to a server              
    gpu                         Manage GPUs attached to a server                
    nic                         Manage network interface cards (NICs) for a     
                                server                                          
    label                       Manage labels on a server                       
    tag                         Manage tags on a server                         
```

## `rpk servers summary`
```
DESCRIPTION:
Show a summarized hardware report for all servers

USAGE:
    rpk servers summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers add`
```
DESCRIPTION:
Add a new server to the inventory

USAGE:
    rpk servers add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers get`
```
DESCRIPTION:
List all servers or retrieve a specific server by name

USAGE:
    rpk servers get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers describe`
```
DESCRIPTION:
Display detailed information about a specific server

USAGE:
    rpk servers describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers set`
```
DESCRIPTION:
Update properties of an existing server

USAGE:
    rpk servers set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --ram <GB>                                
        --ram_mts <MTS>                           
        --ipmi                                    
```

## `rpk servers del`
```
DESCRIPTION:
Delete a server from the inventory

USAGE:
    rpk servers del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers rename`
```
DESCRIPTION:
Rename a server to a new name

USAGE:
    rpk servers rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers tree`
```
DESCRIPTION:
Display the dependency tree of a server

USAGE:
    rpk servers tree <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers cpu`
```
DESCRIPTION:
Manage CPUs attached to a server

USAGE:
    rpk servers cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a CPU to a specific server      
    set <name>    Update configuration of a server CPU
    del <name>    Remove a CPU from a server          
```

## `rpk servers cpu add`
```
DESCRIPTION:
Add a CPU to a specific server

USAGE:
    rpk servers cpu add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help                 Prints help information
        --model <MODEL>                               
        --cores <CORES>                               
        --threads <THREADS>                           
```

## `rpk servers cpu set`
```
DESCRIPTION:
Update configuration of a server CPU

USAGE:
    rpk servers cpu set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help                 Prints help information
        --index <INDEX>                               
        --model <MODEL>                               
        --cores <CORES>                               
        --threads <THREADS>                           
```

## `rpk servers cpu del`
```
DESCRIPTION:
Remove a CPU from a server

USAGE:
    rpk servers cpu del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk servers drive`
```
DESCRIPTION:
Manage drives attached to a server

USAGE:
    rpk servers drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a storage drive to a server    
    set <name>    Update properties of a server drive
    del <name>    Remove a drive from a server       
```

## `rpk servers drive add`
```
DESCRIPTION:
Add a storage drive to a server

USAGE:
    rpk servers drive add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help           Prints help information     
        --type <TYPE>    The drive type e.g hdd / ssd
        --size <SIZE>    The drive capacity in GB    
```

## `rpk servers drive set`
```
DESCRIPTION:
Update properties of a server drive

USAGE:
    rpk servers drive set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --type <TYPE>                             
        --size <SIZE>                             
```

## `rpk servers drive del`
```
DESCRIPTION:
Remove a drive from a server

USAGE:
    rpk servers drive del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk servers gpu`
```
DESCRIPTION:
Manage GPUs attached to a server

USAGE:
    rpk servers gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a GPU to a server            
    set <name>    Update properties of a server GPU
    del <name>    Remove a GPU from a server       
```

## `rpk servers gpu add`
```
DESCRIPTION:
Add a GPU to a server

USAGE:
    rpk servers gpu add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --model <MODEL>                           
        --vram <VRAM>                             
```

## `rpk servers gpu set`
```
DESCRIPTION:
Update properties of a server GPU

USAGE:
    rpk servers gpu set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --model <MODEL>                           
        --vram <VRAM>                             
```

## `rpk servers gpu del`
```
DESCRIPTION:
Remove a GPU from a server

USAGE:
    rpk servers gpu del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk servers nic`
```
DESCRIPTION:
Manage network interface cards (NICs) for a server

USAGE:
    rpk servers nic [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a NIC to a server            
    set <name>    Update properties of a server NIC
    del <name>    Remove a NIC from a server       
```

## `rpk servers nic add`
```
DESCRIPTION:
Add a NIC to a server

USAGE:
    rpk servers nic add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --type <TYPE>                             
        --speed <SPEED>                           
        --ports <PORTS>                           
```

## `rpk servers nic set`
```
DESCRIPTION:
Update properties of a server NIC

USAGE:
    rpk servers nic set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --type <TYPE>                             
        --speed <SPEED>                           
        --ports <PORTS>                           
```

## `rpk servers nic del`
```
DESCRIPTION:
Remove a NIC from a server

USAGE:
    rpk servers nic del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk servers label`
```
DESCRIPTION:
Manage labels on a server

USAGE:
    rpk servers label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a server     
    remove <name>    Remove a label from a server
```

## `rpk servers label add`
```
DESCRIPTION:
Add a label to a server

USAGE:
    rpk servers label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk servers label remove`
```
DESCRIPTION:
Remove a label from a server

USAGE:
    rpk servers label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk servers tag`
```
DESCRIPTION:
Manage tags on a server

USAGE:
    rpk servers tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a server     
    remove <name> <tag>    Remove a tag from a server
```

## `rpk servers tag add`
```
DESCRIPTION:
Add a tag to a server

USAGE:
    rpk servers tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers tag remove`
```
DESCRIPTION:
Remove a tag from a server

USAGE:
    rpk servers tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches`
```
DESCRIPTION:
Manage network switches

USAGE:
    rpk switches [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all switches      
    add <name>                  Add a new network switch to the inventory    
    list                        List all switches in the system              
    get <name>                  Retrieve details of a specific switch by name
    describe <name>             Show detailed information about a switch     
    set <name>                  Update properties of a switch                
    del <name>                  Delete a switch from the inventory           
    rename <name> <new-name>    Rename a switch to a new name                
    port                        Manage ports on a network switch             
    label                       Manage labels on a switch                    
    tag                         Manage tags on a switch                      
```

## `rpk switches summary`
```
DESCRIPTION:
Show a hardware report for all switches

USAGE:
    rpk switches summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches add`
```
DESCRIPTION:
Add a new network switch to the inventory

USAGE:
    rpk switches add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches list`
```
DESCRIPTION:
List all switches in the system

USAGE:
    rpk switches list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches get`
```
DESCRIPTION:
Retrieve details of a specific switch by name

USAGE:
    rpk switches get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches describe`
```
DESCRIPTION:
Show detailed information about a switch

USAGE:
    rpk switches describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches set`
```
DESCRIPTION:
Update properties of a switch

USAGE:
    rpk switches set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help       Prints help information
        --Model                             
        --managed                           
        --poe                               
```

## `rpk switches del`
```
DESCRIPTION:
Delete a switch from the inventory

USAGE:
    rpk switches del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches rename`
```
DESCRIPTION:
Rename a switch to a new name

USAGE:
    rpk switches rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches port`
```
DESCRIPTION:
Manage ports on a network switch

USAGE:
    rpk switches port [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a port to a switch     
    set <name>    Update a switch port       
    del <name>    Remove a port from a switch
```

## `rpk switches port add`
```
DESCRIPTION:
Add a port to a switch

USAGE:
    rpk switches port add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information          
        --type     The port type (e.g., rj45, sfp+) 
        --speed    The port speed (e.g., 1, 2.5, 10)
        --count    Number of ports of this type     
```

## `rpk switches port set`
```
DESCRIPTION:
Update a switch port

USAGE:
    rpk switches port set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --type                                    
        --speed                                   
        --count                                   
```

## `rpk switches port del`
```
DESCRIPTION:
Remove a port from a switch

USAGE:
    rpk switches port del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk switches label`
```
DESCRIPTION:
Manage labels on a switch

USAGE:
    rpk switches label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a switch     
    remove <name>    Remove a label from a switch
```

## `rpk switches label add`
```
DESCRIPTION:
Add a label to a switch

USAGE:
    rpk switches label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk switches label remove`
```
DESCRIPTION:
Remove a label from a switch

USAGE:
    rpk switches label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk switches tag`
```
DESCRIPTION:
Manage tags on a switch

USAGE:
    rpk switches tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a switch     
    remove <name> <tag>    Remove a tag from a switch
```

## `rpk switches tag add`
```
DESCRIPTION:
Add a tag to a switch

USAGE:
    rpk switches tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches tag remove`
```
DESCRIPTION:
Remove a tag from a switch

USAGE:
    rpk switches tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers`
```
DESCRIPTION:
Manage network routers

USAGE:
    rpk routers [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all routers       
    add <name>                  Add a new network router to the inventory    
    list                        List all routers in the system               
    get <name>                  Retrieve details of a specific router by name
    describe <name>             Show detailed information about a router     
    set <name>                  Update properties of a router                
    del <name>                  Delete a router from the inventory           
    rename <name> <new-name>    Rename a router to a new name                
    port                        Manage ports on a router                     
    label                       Manage labels on a router                    
    tag                         Manage tags on a router                      
```

## `rpk routers summary`
```
DESCRIPTION:
Show a hardware report for all routers

USAGE:
    rpk routers summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers add`
```
DESCRIPTION:
Add a new network router to the inventory

USAGE:
    rpk routers add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers list`
```
DESCRIPTION:
List all routers in the system

USAGE:
    rpk routers list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers get`
```
DESCRIPTION:
Retrieve details of a specific router by name

USAGE:
    rpk routers get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers describe`
```
DESCRIPTION:
Show detailed information about a router

USAGE:
    rpk routers describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers set`
```
DESCRIPTION:
Update properties of a router

USAGE:
    rpk routers set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help       Prints help information
        --Model                             
        --managed                           
        --poe                               
```

## `rpk routers del`
```
DESCRIPTION:
Delete a router from the inventory

USAGE:
    rpk routers del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers rename`
```
DESCRIPTION:
Rename a router to a new name

USAGE:
    rpk routers rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers port`
```
DESCRIPTION:
Manage ports on a router

USAGE:
    rpk routers port [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a port to a router     
    set <name>    Update a router port       
    del <name>    Remove a port from a router
```

## `rpk routers port add`
```
DESCRIPTION:
Add a port to a router

USAGE:
    rpk routers port add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information
        --type                            
        --speed                           
        --count                           
```

## `rpk routers port set`
```
DESCRIPTION:
Update a router port

USAGE:
    rpk routers port set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --type                                    
        --speed                                   
        --count                                   
```

## `rpk routers port del`
```
DESCRIPTION:
Remove a port from a router

USAGE:
    rpk routers port del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk routers label`
```
DESCRIPTION:
Manage labels on a router

USAGE:
    rpk routers label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a router     
    remove <name>    Remove a label from a router
```

## `rpk routers label add`
```
DESCRIPTION:
Add a label to a router

USAGE:
    rpk routers label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk routers label remove`
```
DESCRIPTION:
Remove a label from a router

USAGE:
    rpk routers label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk routers tag`
```
DESCRIPTION:
Manage tags on a router

USAGE:
    rpk routers tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a router     
    remove <name> <tag>    Remove a tag from a router
```

## `rpk routers tag add`
```
DESCRIPTION:
Add a tag to a router

USAGE:
    rpk routers tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk routers tag remove`
```
DESCRIPTION:
Remove a tag from a router

USAGE:
    rpk routers tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls`
```
DESCRIPTION:
Manage firewalls

USAGE:
    rpk firewalls [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all firewalls       
    add <name>                  Add a new firewall to the inventory            
    list                        List all firewalls in the system               
    get <name>                  Retrieve details of a specific firewall by name
    describe <name>             Show detailed information about a firewall     
    set <name>                  Update properties of a firewall                
    del <name>                  Delete a firewall from the inventory           
    rename <name> <new-name>    Rename a firewall to a new name                
    port                        Manage ports on a firewall                     
    label                       Manage labels on a firewall                    
    tag                         Manage tags on a firewall                      
```

## `rpk firewalls summary`
```
DESCRIPTION:
Show a hardware report for all firewalls

USAGE:
    rpk firewalls summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls add`
```
DESCRIPTION:
Add a new firewall to the inventory

USAGE:
    rpk firewalls add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls list`
```
DESCRIPTION:
List all firewalls in the system

USAGE:
    rpk firewalls list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls get`
```
DESCRIPTION:
Retrieve details of a specific firewall by name

USAGE:
    rpk firewalls get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls describe`
```
DESCRIPTION:
Show detailed information about a firewall

USAGE:
    rpk firewalls describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls set`
```
DESCRIPTION:
Update properties of a firewall

USAGE:
    rpk firewalls set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help       Prints help information
        --Model                             
        --managed                           
        --poe                               
```

## `rpk firewalls del`
```
DESCRIPTION:
Delete a firewall from the inventory

USAGE:
    rpk firewalls del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls rename`
```
DESCRIPTION:
Rename a firewall to a new name

USAGE:
    rpk firewalls rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls port`
```
DESCRIPTION:
Manage ports on a firewall

USAGE:
    rpk firewalls port [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a port to a firewall     
    set <name>    Update a firewall port       
    del <name>    Remove a port from a firewall
```

## `rpk firewalls port add`
```
DESCRIPTION:
Add a port to a firewall

USAGE:
    rpk firewalls port add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information
        --type                            
        --speed                           
        --count                           
```

## `rpk firewalls port set`
```
DESCRIPTION:
Update a firewall port

USAGE:
    rpk firewalls port set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
        --type                                    
        --speed                                   
        --count                                   
```

## `rpk firewalls port del`
```
DESCRIPTION:
Remove a port from a firewall

USAGE:
    rpk firewalls port del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --index <INDEX>                           
```

## `rpk firewalls label`
```
DESCRIPTION:
Manage labels on a firewall

USAGE:
    rpk firewalls label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a firewall     
    remove <name>    Remove a label from a firewall
```

## `rpk firewalls label add`
```
DESCRIPTION:
Add a label to a firewall

USAGE:
    rpk firewalls label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk firewalls label remove`
```
DESCRIPTION:
Remove a label from a firewall

USAGE:
    rpk firewalls label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk firewalls tag`
```
DESCRIPTION:
Manage tags on a firewall

USAGE:
    rpk firewalls tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a firewall     
    remove <name> <tag>    Remove a tag from a firewall
```

## `rpk firewalls tag add`
```
DESCRIPTION:
Add a tag to a firewall

USAGE:
    rpk firewalls tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk firewalls tag remove`
```
DESCRIPTION:
Remove a tag from a firewall

USAGE:
    rpk firewalls tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems`
```
DESCRIPTION:
Manage systems and their dependencies

USAGE:
    rpk systems [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a summary report for all systems      
    add <name>                  Add a new system to the inventory          
    list                        List all systems                           
    get <name>                  Retrieve a system by name                  
    describe <name>             Display detailed information about a system
    set <name>                  Update properties of a system              
    del <name>                  Delete a system from the inventory         
    rename <name> <new-name>    Rename a system to a new name              
    tree <name>                 Display the dependency tree for a system   
    label                       Manage labels on a system                  
    tag                         Manage tags on a system                    
```

## `rpk systems summary`
```
DESCRIPTION:
Show a summary report for all systems

USAGE:
    rpk systems summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems add`
```
DESCRIPTION:
Add a new system to the inventory

USAGE:
    rpk systems add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems list`
```
DESCRIPTION:
List all systems

USAGE:
    rpk systems list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems get`
```
DESCRIPTION:
Retrieve a system by name

USAGE:
    rpk systems get <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems describe`
```
DESCRIPTION:
Display detailed information about a system

USAGE:
    rpk systems describe <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems set`
```
DESCRIPTION:
Update properties of a system

USAGE:
    rpk systems set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help                Prints help information                          
        --type                                                                 
        --os                                                                   
        --cores                                                                
        --ram                                                                  
        --runs-on <RUNSON>    The physical machine(s) the service is running on
        --ip                  The ip address of the system                     
```

## `rpk systems del`
```
DESCRIPTION:
Delete a system from the inventory

USAGE:
    rpk systems del <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems rename`
```
DESCRIPTION:
Rename a system to a new name

USAGE:
    rpk systems rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>        The name of the system
    <new-name>                          

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems tree`
```
DESCRIPTION:
Display the dependency tree for a system

USAGE:
    rpk systems tree <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems label`
```
DESCRIPTION:
Manage labels on a system

USAGE:
    rpk systems label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a system     
    remove <name>    Remove a label from a system
```

## `rpk systems label add`
```
DESCRIPTION:
Add a label to a system

USAGE:
    rpk systems label add <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk systems label remove`
```
DESCRIPTION:
Remove a label from a system

USAGE:
    rpk systems label remove <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the system

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk systems tag`
```
DESCRIPTION:
Manage tags on a system

USAGE:
    rpk systems tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a system     
    remove <name> <tag>    Remove a tag from a system
```

## `rpk systems tag add`
```
DESCRIPTION:
Add a tag to a system

USAGE:
    rpk systems tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems tag remove`
```
DESCRIPTION:
Remove a tag from a system

USAGE:
    rpk systems tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints`
```
DESCRIPTION:
Manage access points

USAGE:
    rpk accesspoints [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all access points   
    add <name>                  Add a new access point                         
    list                        List all access points                         
    get <name>                  Retrieve an access point by name               
    describe <name>             Show detailed information about an access point
    set <name>                  Update properties of an access point           
    del <name>                  Delete an access point                         
    rename <name> <new-name>    Rename an access point to a new name           
    label                       Manage labels on an access point               
    tag                         Manage tags on an access point                 
```

## `rpk accesspoints summary`
```
DESCRIPTION:
Show a hardware report for all access points

USAGE:
    rpk accesspoints summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints add`
```
DESCRIPTION:
Add a new access point

USAGE:
    rpk accesspoints add <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints list`
```
DESCRIPTION:
List all access points

USAGE:
    rpk accesspoints list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints get`
```
DESCRIPTION:
Retrieve an access point by name

USAGE:
    rpk accesspoints get <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints describe`
```
DESCRIPTION:
Show detailed information about an access point

USAGE:
    rpk accesspoints describe <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints set`
```
DESCRIPTION:
Update properties of an access point

USAGE:
    rpk accesspoints set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information            
        --model    The access point model name        
        --speed    The speed of the access point in Gb
```

## `rpk accesspoints del`
```
DESCRIPTION:
Delete an access point

USAGE:
    rpk accesspoints del <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints rename`
```
DESCRIPTION:
Rename an access point to a new name

USAGE:
    rpk accesspoints rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>        The access point name
    <new-name>                         

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints label`
```
DESCRIPTION:
Manage labels on an access point

USAGE:
    rpk accesspoints label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to an access point     
    remove <name>    Remove a label from an access point
```

## `rpk accesspoints label add`
```
DESCRIPTION:
Add a label to an access point

USAGE:
    rpk accesspoints label add <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk accesspoints label remove`
```
DESCRIPTION:
Remove a label from an access point

USAGE:
    rpk accesspoints label remove <name> [OPTIONS]

ARGUMENTS:
    <name>    The access point name

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk accesspoints tag`
```
DESCRIPTION:
Manage tags on an access point

USAGE:
    rpk accesspoints tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to an access point     
    remove <name> <tag>    Remove a tag from an access point
```

## `rpk accesspoints tag add`
```
DESCRIPTION:
Add a tag to an access point

USAGE:
    rpk accesspoints tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints tag remove`
```
DESCRIPTION:
Remove a tag from an access point

USAGE:
    rpk accesspoints tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups`
```
DESCRIPTION:
Manage UPS units

USAGE:
    rpk ups [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all UPS units  
    add <name>                  Add a new UPS unit                        
    list                        List all UPS units                        
    get <name>                  Retrieve a UPS unit by name               
    describe <name>             Show detailed information about a UPS unit
    set <name>                  Update properties of a UPS unit           
    del <name>                  Delete a UPS unit                         
    rename <name> <new-name>    Rename a UPS unit to a new name           
    label                       Manage labels on a UPS unit               
    tag                         Manage tags on a UPS unit                 
```

## `rpk ups summary`
```
DESCRIPTION:
Show a hardware report for all UPS units

USAGE:
    rpk ups summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups add`
```
DESCRIPTION:
Add a new UPS unit

USAGE:
    rpk ups add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups list`
```
DESCRIPTION:
List all UPS units

USAGE:
    rpk ups list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups get`
```
DESCRIPTION:
Retrieve a UPS unit by name

USAGE:
    rpk ups get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups describe`
```
DESCRIPTION:
Show detailed information about a UPS unit

USAGE:
    rpk ups describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups set`
```
DESCRIPTION:
Update properties of a UPS unit

USAGE:
    rpk ups set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information
        --model                           
        --va                              
```

## `rpk ups del`
```
DESCRIPTION:
Delete a UPS unit

USAGE:
    rpk ups del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups rename`
```
DESCRIPTION:
Rename a UPS unit to a new name

USAGE:
    rpk ups rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups label`
```
DESCRIPTION:
Manage labels on a UPS unit

USAGE:
    rpk ups label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a UPS unit     
    remove <name>    Remove a label from a UPS unit
```

## `rpk ups label add`
```
DESCRIPTION:
Add a label to a UPS unit

USAGE:
    rpk ups label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk ups label remove`
```
DESCRIPTION:
Remove a label from a UPS unit

USAGE:
    rpk ups label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk ups tag`
```
DESCRIPTION:
Manage tags on a UPS unit

USAGE:
    rpk ups tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a UPS unit     
    remove <name> <tag>    Remove a tag from a UPS unit
```

## `rpk ups tag add`
```
DESCRIPTION:
Add a tag to a UPS unit

USAGE:
    rpk ups tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups tag remove`
```
DESCRIPTION:
Remove a tag from a UPS unit

USAGE:
    rpk ups tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other`
```
DESCRIPTION:
Manage other hardware that doesn't fit an existing category

USAGE:
    rpk other [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a hardware report for all other hardware 
    add <name>                  Add new other hardware                        
    list                        List all other hardware                       
    get <name>                  Retrieve other hardware by name               
    describe <name>             Show detailed information about other hardware
    set <name>                  Update properties of other hardware           
    del <name>                  Delete other hardware                         
    rename <name> <new-name>    Rename other hardware to a new name           
    label                       Manage labels on other hardware               
    tag                         Manage tags on other hardware                 
```

## `rpk other summary`
```
DESCRIPTION:
Show a hardware report for all other hardware

USAGE:
    rpk other summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other add`
```
DESCRIPTION:
Add new other hardware

USAGE:
    rpk other add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other list`
```
DESCRIPTION:
List all other hardware

USAGE:
    rpk other list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other get`
```
DESCRIPTION:
Retrieve other hardware by name

USAGE:
    rpk other get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other describe`
```
DESCRIPTION:
Show detailed information about other hardware

USAGE:
    rpk other describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other set`
```
DESCRIPTION:
Update properties of other hardware

USAGE:
    rpk other set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help           Prints help information
        --model                                 
        --description                           
```

## `rpk other del`
```
DESCRIPTION:
Delete other hardware

USAGE:
    rpk other del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other rename`
```
DESCRIPTION:
Rename other hardware to a new name

USAGE:
    rpk other rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other label`
```
DESCRIPTION:
Manage labels on other hardware

USAGE:
    rpk other label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to other hardware     
    remove <name>    Remove a label from other hardware
```

## `rpk other label add`
```
DESCRIPTION:
Add a label to other hardware

USAGE:
    rpk other label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk other label remove`
```
DESCRIPTION:
Remove a label from other hardware

USAGE:
    rpk other label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk other tag`
```
DESCRIPTION:
Manage tags on other hardware

USAGE:
    rpk other tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to other hardware     
    remove <name> <tag>    Remove a tag from other hardware
```

## `rpk other tag add`
```
DESCRIPTION:
Add a tag to other hardware

USAGE:
    rpk other tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk other tag remove`
```
DESCRIPTION:
Remove a tag from other hardware

USAGE:
    rpk other tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops`
```
DESCRIPTION:
Manage desktop computers and their components

USAGE:
    rpk desktops [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>                  Add a new desktop                               
    list                        List all desktops                               
    get <name>                  Retrieve a desktop by name                      
    describe <name>             Show detailed information about a desktop       
    set <name>                  Update properties of a desktop                  
    del <name>                  Delete a desktop from the inventory             
    rename <name> <new-name>    Rename a desktop to a new name                  
    summary                     Show a summarized hardware report for all       
                                desktops                                        
    tree <name>                 Display the dependency tree for a desktop       
    cpu                         Manage CPUs attached to desktops                
    drive                       Manage storage drives attached to desktops      
    gpu                         Manage GPUs attached to desktops                
    nic                         Manage network interface cards (NICs) for       
                                desktops                                        
    label                       Manage labels on a desktop                      
    tag                         Manage tags on a desktop                        
```

## `rpk desktops add`
```
DESCRIPTION:
Add a new desktop

USAGE:
    rpk desktops add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops list`
```
DESCRIPTION:
List all desktops

USAGE:
    rpk desktops list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops get`
```
DESCRIPTION:
Retrieve a desktop by name

USAGE:
    rpk desktops get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops describe`
```
DESCRIPTION:
Show detailed information about a desktop

USAGE:
    rpk desktops describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops set`
```
DESCRIPTION:
Update properties of a desktop

USAGE:
    rpk desktops set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information
        --model                           
```

## `rpk desktops del`
```
DESCRIPTION:
Delete a desktop from the inventory

USAGE:
    rpk desktops del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops rename`
```
DESCRIPTION:
Rename a desktop to a new name

USAGE:
    rpk desktops rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops summary`
```
DESCRIPTION:
Show a summarized hardware report for all desktops

USAGE:
    rpk desktops summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops tree`
```
DESCRIPTION:
Display the dependency tree for a desktop

USAGE:
    rpk desktops tree <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops cpu`
```
DESCRIPTION:
Manage CPUs attached to desktops

USAGE:
    rpk desktops cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>            Add a CPU to a desktop     
    set <desktop> <index>    Update a desktop CPU       
    del <desktop> <index>    Remove a CPU from a desktop
```

## `rpk desktops cpu add`
```
DESCRIPTION:
Add a CPU to a desktop

USAGE:
    rpk desktops cpu add <desktop> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name

OPTIONS:
    -h, --help       Prints help information  
        --model      The model name           
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk desktops cpu set`
```
DESCRIPTION:
Update a desktop CPU

USAGE:
    rpk desktops cpu set <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name            
    <index>      The index of the desktop cpu

OPTIONS:
    -h, --help       Prints help information  
        --model      The cpu model            
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk desktops cpu del`
```
DESCRIPTION:
Remove a CPU from a desktop

USAGE:
    rpk desktops cpu del <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The name of the desktop               
    <index>      The index of the desktop cpu to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops drive`
```
DESCRIPTION:
Manage storage drives attached to desktops

USAGE:
    rpk desktops drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>            Add a drive to a desktop     
    set <desktop> <index>    Update a desktop drive       
    del <desktop> <index>    Remove a drive from a desktop
```

## `rpk desktops drive add`
```
DESCRIPTION:
Add a drive to a desktop

USAGE:
    rpk desktops drive add <desktop> [OPTIONS]

ARGUMENTS:
    <desktop>    The name of the desktop

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in GB    
```

## `rpk desktops drive set`
```
DESCRIPTION:
Update a desktop drive

USAGE:
    rpk desktops drive set <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name         
    <index>      The drive index to update

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in Gb    
```

## `rpk desktops drive del`
```
DESCRIPTION:
Remove a drive from a desktop

USAGE:
    rpk desktops drive del <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The name of the desktop         
    <index>      The index of the drive to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops gpu`
```
DESCRIPTION:
Manage GPUs attached to desktops

USAGE:
    rpk desktops gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>            Add a GPU to a desktop     
    set <desktop> <index>    Update a desktop GPU       
    del <desktop> <index>    Remove a GPU from a desktop
```

## `rpk desktops gpu add`
```
DESCRIPTION:
Add a GPU to a desktop

USAGE:
    rpk desktops gpu add <desktop> [OPTIONS]

ARGUMENTS:
    <desktop>    The name of the desktop

OPTIONS:
    -h, --help     Prints help information     
        --model    The Gpu model               
        --vram     The amount of gpu vram in Gb
```

## `rpk desktops gpu set`
```
DESCRIPTION:
Update a desktop GPU

USAGE:
    rpk desktops gpu set <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name              
    <index>      The index of the gpu to update

OPTIONS:
    -h, --help     Prints help information     
        --model    The gpu model name          
        --vram     The amount of gpu vram in Gb
```

## `rpk desktops gpu del`
```
DESCRIPTION:
Remove a GPU from a desktop

USAGE:
    rpk desktops gpu del <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name              
    <index>      The index of the Gpu to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops nic`
```
DESCRIPTION:
Manage network interface cards (NICs) for desktops

USAGE:
    rpk desktops nic [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>            Add a NIC to a desktop     
    set <desktop> <index>    Update a desktop NIC       
    del <desktop> <index>    Remove a NIC from a desktop
```

## `rpk desktops nic add`
```
DESCRIPTION:
Add a NIC to a desktop

USAGE:
    rpk desktops nic add <desktop> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name

OPTIONS:
    -h, --help     Prints help information          
        --type     The nic port type e.g rj45 / sfp+
        --speed    The port speed                   
        --ports    The number of ports              
```

## `rpk desktops nic set`
```
DESCRIPTION:
Update a desktop NIC

USAGE:
    rpk desktops nic set <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name              
    <index>      The index of the nic to remove

OPTIONS:
    -h, --help     Prints help information          
        --type     The nic port type e.g rj45 / sfp+
        --speed    The speed of the nic in Gb/s     
        --ports    The number of ports              
```

## `rpk desktops nic del`
```
DESCRIPTION:
Remove a NIC from a desktop

USAGE:
    rpk desktops nic del <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name              
    <index>      The index of the nic to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops label`
```
DESCRIPTION:
Manage labels on a desktop

USAGE:
    rpk desktops label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a desktop     
    remove <name>    Remove a label from a desktop
```

## `rpk desktops label add`
```
DESCRIPTION:
Add a label to a desktop

USAGE:
    rpk desktops label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk desktops label remove`
```
DESCRIPTION:
Remove a label from a desktop

USAGE:
    rpk desktops label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk desktops tag`
```
DESCRIPTION:
Manage tags on a desktop

USAGE:
    rpk desktops tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a desktop     
    remove <name> <tag>    Remove a tag from a desktop
```

## `rpk desktops tag add`
```
DESCRIPTION:
Add a tag to a desktop

USAGE:
    rpk desktops tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops tag remove`
```
DESCRIPTION:
Remove a tag from a desktop

USAGE:
    rpk desktops tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops`
```
DESCRIPTION:
Manage Laptop computers and their components

USAGE:
    rpk laptops [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>                  Add a new Laptop                                
    list                        List all Laptops                                
    get <name>                  Retrieve a Laptop by name                       
    describe <name>             Show detailed information about a Laptop        
    set <name>                  Update properties of a laptop                   
    del <name>                  Delete a Laptop from the inventory              
    rename <name> <new-name>    Rename a Laptop to a new name                   
    summary                     Show a summarized hardware report for all       
                                Laptops                                         
    tree <name>                 Display the dependency tree for a Laptop        
    cpu                         Manage CPUs attached to Laptops                 
    drive                       Manage storage drives attached to Laptops       
    gpu                         Manage GPUs attached to Laptops                 
    label                       Manage labels on a laptop                       
    tag                         Manage tags on a laptop                         
```

## `rpk laptops add`
```
DESCRIPTION:
Add a new Laptop

USAGE:
    rpk laptops add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops list`
```
DESCRIPTION:
List all Laptops

USAGE:
    rpk laptops list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops get`
```
DESCRIPTION:
Retrieve a Laptop by name

USAGE:
    rpk laptops get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops describe`
```
DESCRIPTION:
Show detailed information about a Laptop

USAGE:
    rpk laptops describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops set`
```
DESCRIPTION:
Update properties of a laptop

USAGE:
    rpk laptops set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help     Prints help information
        --model                           
```

## `rpk laptops del`
```
DESCRIPTION:
Delete a Laptop from the inventory

USAGE:
    rpk laptops del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops rename`
```
DESCRIPTION:
Rename a Laptop to a new name

USAGE:
    rpk laptops rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>         
    <new-name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops summary`
```
DESCRIPTION:
Show a summarized hardware report for all Laptops

USAGE:
    rpk laptops summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops tree`
```
DESCRIPTION:
Display the dependency tree for a Laptop

USAGE:
    rpk laptops tree <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops cpu`
```
DESCRIPTION:
Manage CPUs attached to Laptops

USAGE:
    rpk laptops cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <Laptop>            Add a CPU to a Laptop     
    set <Laptop> <index>    Update a Laptop CPU       
    del <Laptop> <index>    Remove a CPU from a Laptop
```

## `rpk laptops cpu add`
```
DESCRIPTION:
Add a CPU to a Laptop

USAGE:
    rpk laptops cpu add <Laptop> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name

OPTIONS:
    -h, --help       Prints help information  
        --model      The model name           
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk laptops cpu set`
```
DESCRIPTION:
Update a Laptop CPU

USAGE:
    rpk laptops cpu set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name            
    <index>     The index of the Laptop cpu

OPTIONS:
    -h, --help       Prints help information  
        --model      The cpu model            
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk laptops cpu del`
```
DESCRIPTION:
Remove a CPU from a Laptop

USAGE:
    rpk laptops cpu del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop               
    <index>     The index of the Laptop cpu to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops drive`
```
DESCRIPTION:
Manage storage drives attached to Laptops

USAGE:
    rpk laptops drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <laptop>            Add a drive to a Laptop     
    set <Laptop> <index>    Update a Laptop drive       
    del <Laptop> <index>    Remove a drive from a Laptop
```

## `rpk laptops drive add`
```
DESCRIPTION:
Add a drive to a Laptop

USAGE:
    rpk laptops drive add <laptop> [OPTIONS]

ARGUMENTS:
    <laptop>    The name of the Laptop

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in GB:   
```

## `rpk laptops drive set`
```
DESCRIPTION:
Update a Laptop drive

USAGE:
    rpk laptops drive set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name          
    <index>     The drive index to update

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in Gb    
```

## `rpk laptops drive del`
```
DESCRIPTION:
Remove a drive from a Laptop

USAGE:
    rpk laptops drive del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop          
    <index>     The index of the drive to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops gpu`
```
DESCRIPTION:
Manage GPUs attached to Laptops

USAGE:
    rpk laptops gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <Laptop>            Add a GPU to a Laptop     
    set <Laptop> <index>    Update a Laptop GPU       
    del <Laptop> <index>    Remove a GPU from a Laptop
```

## `rpk laptops gpu add`
```
DESCRIPTION:
Add a GPU to a Laptop

USAGE:
    rpk laptops gpu add <Laptop> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop

OPTIONS:
    -h, --help     Prints help information     
        --model    The Gpu model               
        --vram     The amount of gpu vram in Gb
```

## `rpk laptops gpu set`
```
DESCRIPTION:
Update a Laptop GPU

USAGE:
    rpk laptops gpu set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name               
    <index>     The index of the gpu to update

OPTIONS:
    -h, --help     Prints help information     
        --model    The gpu model name          
        --vram     The amount of gpu vram in Gb
```

## `rpk laptops gpu del`
```
DESCRIPTION:
Remove a GPU from a Laptop

USAGE:
    rpk laptops gpu del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name               
    <index>     The index of the Gpu to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops label`
```
DESCRIPTION:
Manage labels on a laptop

USAGE:
    rpk laptops label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a laptop     
    remove <name>    Remove a label from a laptop
```

## `rpk laptops label add`
```
DESCRIPTION:
Add a label to a laptop

USAGE:
    rpk laptops label add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk laptops label remove`
```
DESCRIPTION:
Remove a label from a laptop

USAGE:
    rpk laptops label remove <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk laptops tag`
```
DESCRIPTION:
Manage tags on a laptop

USAGE:
    rpk laptops tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a laptop     
    remove <name> <tag>    Remove a tag from a laptop
```

## `rpk laptops tag add`
```
DESCRIPTION:
Add a tag to a laptop

USAGE:
    rpk laptops tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk laptops tag remove`
```
DESCRIPTION:
Remove a tag from a laptop

USAGE:
    rpk laptops tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services`
```
DESCRIPTION:
Manage services and their configurations

USAGE:
    rpk services [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary                     Show a summary report for all services          
    add <name>                  Add a new service                               
    list                        List all services                               
    get <name>                  Retrieve a service by name                      
    describe <name>             Show detailed information about a service       
    set <name>                  Update properties of a service                  
    del <name>                  Delete a service                                
    rename <name> <new-name>    Rename a service to a new name                  
    subnets                     List subnets associated with a service,         
                                optionally filtered by CIDR                     
    label                       Manage labels on a service                      
    tag                         Manage tags on a service                        
```

## `rpk services summary`
```
DESCRIPTION:
Show a summary report for all services

USAGE:
    rpk services summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services add`
```
DESCRIPTION:
Add a new service

USAGE:
    rpk services add <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services list`
```
DESCRIPTION:
List all services

USAGE:
    rpk services list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services get`
```
DESCRIPTION:
Retrieve a service by name

USAGE:
    rpk services get <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services describe`
```
DESCRIPTION:
Show detailed information about a service

USAGE:
    rpk services describe <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services set`
```
DESCRIPTION:
Update properties of a service

USAGE:
    rpk services set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help                Prints help information                
        --ip                  The ip address of the service          
        --port                The port the service is running on     
        --protocol            The service protocol                   
        --url                 The service URL                        
        --runs-on <RUNSON>    The system(s) the service is running on
```

## `rpk services del`
```
DESCRIPTION:
Delete a service

USAGE:
    rpk services del <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services rename`
```
DESCRIPTION:
Rename a service to a new name

USAGE:
    rpk services rename <name> <new-name> [OPTIONS]

ARGUMENTS:
    <name>        The name of the service
    <new-name>                           

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services subnets`
```
DESCRIPTION:
List subnets associated with a service, optionally filtered by CIDR

USAGE:
    rpk services subnets [OPTIONS]

OPTIONS:
    -h, --help               Prints help information
        --cidr <CIDR>                               
        --prefix <PREFIX>                           
```

## `rpk services label`
```
DESCRIPTION:
Manage labels on a service

USAGE:
    rpk services label [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>       Add a label to a service     
    remove <name>    Remove a label from a service
```

## `rpk services label add`
```
DESCRIPTION:
Add a label to a service

USAGE:
    rpk services label add <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help             Prints help information
        --key <KEY>                               
        --value <VALUE>                           
```

## `rpk services label remove`
```
DESCRIPTION:
Remove a label from a service

USAGE:
    rpk services label remove <name> [OPTIONS]

ARGUMENTS:
    <name>    The name of the service

OPTIONS:
    -h, --help         Prints help information
        --key <KEY>                           
```

## `rpk services tag`
```
DESCRIPTION:
Manage tags on a service

USAGE:
    rpk services tag [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name> <tag>       Add a tag to a service     
    remove <name> <tag>    Remove a tag from a service
```

## `rpk services tag add`
```
DESCRIPTION:
Add a tag to a service

USAGE:
    rpk services tag add <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services tag remove`
```
DESCRIPTION:
Remove a tag from a service

USAGE:
    rpk services tag remove <name> <tag> [OPTIONS]

ARGUMENTS:
    <name>     
    <tag>      

OPTIONS:
    -h, --help    Prints help information
```

## `rpk discover`
```
DESCRIPTION:
Read infrastructure and emit it as RackPeek YAML

USAGE:
    rpk discover [OPTIONS] <COMMAND>

EXAMPLES:
    rpk discover system
    rpk discover system --name nas01 --push
    rpk discover docker
    rpk discover docker --push
    rpk discover proxmox --host https://pve.lan:8006 --insecure

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    system     Inspect this machine and emit it as a System resource            
    docker     Read the Docker API and emit each published container as a       
               Service on this host's System                                    
    proxmox    Read a Proxmox cluster and emit its nodes and guests as Systems  
```

## `rpk discover system`
```
DESCRIPTION:
Inspect this machine and emit it as a System resource

USAGE:
    rpk discover system [OPTIONS]

EXAMPLES:
    rpk discover system
    rpk discover system --name nas01 --push

OPTIONS:
    -h, --help             Prints help information                              
        --push             Upload the result to a RackPeek server instead of    
                           printing it                                          
        --server <URL>     RackPeek server to upload to. Defaults to the        
                           RPK_SERVER environment variable                      
        --api-key <KEY>    API key for the server. Defaults to the RPK_API_KEY  
                           environment variable                                 
        --dry-run          Ask the server what would change, without changing   
                           anything. Implies --push                             
    -n, --name <NAME>      Name for this machine. Defaults to its hostname.     
                           Recommended when running from a timer                
```

## `rpk discover docker`
```
DESCRIPTION:
Read the Docker API and emit each published container as a Service on this 
host's System

USAGE:
    rpk discover docker [OPTIONS]

EXAMPLES:
    rpk discover docker
    rpk discover docker --push

OPTIONS:
    -h, --help                 Prints help information                          
        --push                 Upload the result to a RackPeek server instead of
                               printing it                                      
        --server <URL>         RackPeek server to upload to. Defaults to the    
                               RPK_SERVER environment variable                  
        --api-key <KEY>        API key for the server. Defaults to the          
                               RPK_API_KEY environment variable                 
        --dry-run              Ask the server what would change, without        
                               changing anything. Implies --push                
        --docker-host <URI>    Docker endpoint, e.g. unix:///var/run/docker.sock
                               or tcp://host:2375. Defaults to DOCKER_HOST, then
                               the local socket                                 
        --host <NAME>          Name of the machine these containers run on.     
                               Defaults to its hostname                         
```

## `rpk discover proxmox`
```
DESCRIPTION:
Read a Proxmox cluster and emit its nodes and guests as Systems

USAGE:
    rpk discover proxmox [OPTIONS]

EXAMPLES:
    rpk discover proxmox --host https://pve.lan:8006 --insecure
    rpk discover proxmox --host pve.lan --push

OPTIONS:
    -h, --help                     Prints help information                      
        --push                     Upload the result to a RackPeek server       
                                   instead of printing it                       
        --server <URL>             RackPeek server to upload to. Defaults to the
                                   RPK_SERVER environment variable              
        --api-key <KEY>            API key for the server. Defaults to the      
                                   RPK_API_KEY environment variable             
        --dry-run                  Ask the server what would change, without    
                                   changing anything. Implies --push            
        --host <URL>               Proxmox host, e.g. https://pve.lan:8006. A   
                                   bare host name gets https and :8006          
        --token-id <ID>            API token id, e.g. root@pam!rackpeek.        
                                   Defaults to RPK_PVE_TOKEN_ID                 
        --token-secret <SECRET>    API token secret. Defaults to                
                                   RPK_PVE_TOKEN_SECRET                         
        --insecure                 Accept a self-signed certificate, which      
                                   Proxmox ships with by default                
```

## `rpk ansible`
```
DESCRIPTION:
Generate and manage Ansible inventory

USAGE:
    rpk ansible [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    inventory    Generate an Ansible inventory
```

## `rpk ansible inventory`
```
DESCRIPTION:
Generate an Ansible inventory

USAGE:
    rpk ansible inventory [OPTIONS]

OPTIONS:
                          DEFAULT                                               
    -h, --help                       Prints help information                    
        --group-tags                 Comma-separated list of tags to group by   
                                     (e.g. prod,staging)                        
        --group-labels               Comma-separated list of label keys to group
                                     by (e.g. env,site)                         
        --global-var                 Global variable (repeatable). Format:      
                                     key=value                                  
        --format          ini        Inventory format: ini (default) or yaml    
    -o, --output                     Write inventory to file instead of stdout  
```

## `rpk ssh`
```
DESCRIPTION:
Generate SSH configuration from infrastructure

USAGE:
    rpk ssh [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    export    Generate an SSH config file
```

## `rpk ssh export`
```
DESCRIPTION:
Generate an SSH config file

USAGE:
    rpk ssh export [OPTIONS]

OPTIONS:
                              DEFAULT                                           
    -h, --help                           Prints help information                
        --include-tags                   Comma-separated list of tags to include
                                         (e.g. prod,linux)                      
        --default-user                   Default SSH user if not defined in     
                                         labels                                 
        --default-port        22         Default SSH port if not defined in     
                                         labels (default: 22)                   
        --default-identity               Default SSH identity file (e.g.        
                                         ~/.ssh/id_rsa)                         
    -o, --output                         Write SSH config to file instead of    
                                         stdout                                 
```

## `rpk hosts`
```
DESCRIPTION:
Generate a hosts file from infrastructure

USAGE:
    rpk hosts [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    export    Generate a /etc/hosts compatible file
```

## `rpk hosts export`
```
DESCRIPTION:
Generate a /etc/hosts compatible file

USAGE:
    rpk hosts export [OPTIONS]

OPTIONS:
    -h, --help             Prints help information                              
        --include-tags     Comma-separated list of tags to include (e.g.        
                           prod,staging)                                        
        --domain-suffix    Optional domain suffix to append (e.g. home.local)   
        --no-localhost     Do not include localhost defaults                    
    -o, --output           Write hosts file to file instead of stdout           
```

## `rpk graph`
```
DESCRIPTION:
Render inventory as graph diagrams

USAGE:
    rpk graph [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    topology    Emit a Mermaid flowchart of the physical topology (hardware +   
                connections)                                                    
    logical     Emit a Mermaid flowchart of services & systems grouped by subnet
                and host                                                        
```

## `rpk graph topology`
```
DESCRIPTION:
Emit a Mermaid flowchart of the physical topology (hardware + connections)

USAGE:
    rpk graph topology [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk graph logical`
```
DESCRIPTION:
Emit a Mermaid flowchart of services & systems grouped by subnet and host

USAGE:
    rpk graph logical [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk tags`
```
DESCRIPTION:
Discover tags across resources

USAGE:
    rpk tags [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    list          List all tags in use with usage counts
    show <tag>    List resources carrying a specific tag
```

## `rpk tags list`
```
DESCRIPTION:
List all tags in use with usage counts

USAGE:
    rpk tags list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk tags show`
```
DESCRIPTION:
List resources carrying a specific tag

USAGE:
    rpk tags show <tag> [OPTIONS]

ARGUMENTS:
    <tag>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk connections`
```
DESCRIPTION:
Manage physical or logical port connections

USAGE:
    rpk connections [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <A_RESOURCE> <A_GROUP> <A_INDEX> <B_RESOURCE> <B_GROUP> <B_INDEX>    Cre
                                                                             ate
                                                                             a  
                                                                             con
                                                                             nec
                                                                             tio
                                                                             n  
                                                                             bet
                                                                             wee
                                                                             n  
                                                                             two
                                                                             por
                                                                             ts 
    remove <RESOURCE> <PORT_GROUP> <PORT_INDEX>                              Rem
                                                                             ove
                                                                             the
                                                                             con
                                                                             nec
                                                                             tio
                                                                             n  
                                                                             fro
                                                                             m a
                                                                             spe
                                                                             cif
                                                                             ic 
                                                                             por
                                                                             t  
```

## `rpk connections add`
```
DESCRIPTION:
Create a connection between two ports

USAGE:
    rpk connections add <A_RESOURCE> <A_GROUP> <A_INDEX> <B_RESOURCE> <B_GROUP> 
<B_INDEX> [OPTIONS]

ARGUMENTS:
    <A_RESOURCE>    Resource name for endpoint A   
    <A_GROUP>       Port group index for endpoint A
    <A_INDEX>       Port index for endpoint A      
    <B_RESOURCE>    Resource name for endpoint B   
    <B_GROUP>       Port group index for endpoint B
    <B_INDEX>       Port index for endpoint B      

OPTIONS:
    -h, --help     Prints help information          
        --label    Optional label for the connection
        --notes    Optional notes for the connection
```

## `rpk connections remove`
```
DESCRIPTION:
Remove the connection from a specific port

USAGE:
    rpk connections remove <RESOURCE> <PORT_GROUP> <PORT_INDEX> [OPTIONS]

ARGUMENTS:
    <RESOURCE>      Resource name   
    <PORT_GROUP>    Port group index
    <PORT_INDEX>    Port index      

OPTIONS:
    -h, --help    Prints help information
```

