# CLI Commands

## `rpk`
```
USAGE:
    rpk [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary         Show a summarized report of all resources in the system
    servers         Manage servers and their components                    
    switches        Manage network switches                                
    routers         Manage network routers                                 
    firewalls       Manage firewalls                                       
    systems         Manage systems and their dependencies                  
    accesspoints    Manage access points                                   
    ups             Manage UPS units                                       
    desktops        Manage desktop computers and their components          
    Laptops         Manage Laptop computers and their components           
    services        Manage services and their configurations               
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
    summary            Show a summarized hardware report for all servers     
    add <name>         Add a new server to the inventory                     
    get <name>         List all servers or retrieve a specific server by name
    describe <name>    Display detailed information about a specific server  
    set <name>         Update properties of an existing server               
    del <name>         Delete a server from the inventory                    
    tree <name>        Display the dependency tree of a server               
    cpu                Manage CPUs attached to a server                      
    drive              Manage drives attached to a server                    
    gpu                Manage GPUs attached to a server                      
    nic                Manage network interface cards (NICs) for a server    
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
        --type <TYPE>                           
        --size <SIZE>                           
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

## `rpk switches`
```
DESCRIPTION:
Manage network switches

USAGE:
    rpk switches [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a hardware report for all switches      
    add <name>         Add a new network switch to the inventory    
    list               List all switches in the system              
    get <name>         Retrieve details of a specific switch by name
    describe <name>    Show detailed information about a switch     
    set <name>         Update properties of a switch                
    del <name>         Delete a switch from the inventory           
    port               Manage ports on a network switch             
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

## `rpk routers`
```
DESCRIPTION:
Manage network routers

USAGE:
    rpk routers [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a hardware report for all routers       
    add <name>         Add a new network router to the inventory    
    list               List all routers in the system               
    get <name>         Retrieve details of a specific router by name
    describe <name>    Show detailed information about a router     
    set <name>         Update properties of a router                
    del <name>         Delete a router from the inventory           
    port               Manage ports on a router                     
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

## `rpk firewalls`
```
DESCRIPTION:
Manage firewalls

USAGE:
    rpk firewalls [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a hardware report for all firewalls       
    add <name>         Add a new firewall to the inventory            
    list               List all firewalls in the system               
    get <name>         Retrieve details of a specific firewall by name
    describe <name>    Show detailed information about a firewall     
    set <name>         Update properties of a firewall                
    del <name>         Delete a firewall from the inventory           
    port               Manage ports on a firewall                     
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

## `rpk systems`
```
DESCRIPTION:
Manage systems and their dependencies

USAGE:
    rpk systems [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a summary report for all systems      
    add <name>         Add a new system to the inventory          
    list               List all systems                           
    get <name>         Retrieve a system by name                  
    describe <name>    Display detailed information about a system
    set <name>         Update properties of a system              
    del <name>         Delete a system from the inventory         
    tree <name>        Display the dependency tree for a system   
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
    -h, --help       Prints help information
        --type                              
        --os                                
        --cores                             
        --ram                               
        --runs-on                           
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

## `rpk accesspoints`
```
DESCRIPTION:
Manage access points

USAGE:
    rpk accesspoints [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a hardware report for all access points   
    add <name>         Add a new access point                         
    list               List all access points                         
    get <name>         Retrieve an access point by name               
    describe <name>    Show detailed information about an access point
    set <name>         Update properties of an access point           
    del <name>         Delete an access point                         
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

## `rpk ups`
```
DESCRIPTION:
Manage UPS units

USAGE:
    rpk ups [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a hardware report for all UPS units  
    add <name>         Add a new UPS unit                        
    list               List all UPS units                        
    get <name>         Retrieve a UPS unit by name               
    describe <name>    Show detailed information about a UPS unit
    set <name>         Update properties of a UPS unit           
    del <name>         Delete a UPS unit                         
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

## `rpk desktops`
```
DESCRIPTION:
Manage desktop computers and their components

USAGE:
    rpk desktops [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>         Add a new desktop                                 
    list               List all desktops                                 
    get <name>         Retrieve a desktop by name                        
    describe <name>    Show detailed information about a desktop         
    set <name>         Update properties of a desktop                    
    del <name>         Delete a desktop from the inventory               
    summary            Show a summarized hardware report for all desktops
    tree <name>        Display the dependency tree for a desktop         
    cpu                Manage CPUs attached to desktops                  
    drive              Manage storage drives attached to desktops        
    gpu                Manage GPUs attached to desktops                  
    nic                Manage network interface cards (NICs) for desktops
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
        --size    The drive capacity in Gb    
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

## `rpk Laptops`
```
DESCRIPTION:
Manage Laptop computers and their components

USAGE:
    rpk Laptops [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>         Add a new Laptop                                 
    list               List all Laptops                                 
    get <name>         Retrieve a Laptop by name                        
    describe <name>    Show detailed information about a Laptop         
    del <name>         Delete a Laptop from the inventory               
    summary            Show a summarized hardware report for all Laptops
    tree <name>        Display the dependency tree for a Laptop         
    cpu                Manage CPUs attached to Laptops                  
    drive              Manage storage drives attached to Laptops        
    gpu                Manage GPUs attached to Laptops                  
```

## `rpk Laptops add`
```
DESCRIPTION:
Add a new Laptop

USAGE:
    rpk Laptops add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops list`
```
DESCRIPTION:
List all Laptops

USAGE:
    rpk Laptops list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops get`
```
DESCRIPTION:
Retrieve a Laptop by name

USAGE:
    rpk Laptops get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops describe`
```
DESCRIPTION:
Show detailed information about a Laptop

USAGE:
    rpk Laptops describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops del`
```
DESCRIPTION:
Delete a Laptop from the inventory

USAGE:
    rpk Laptops del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops summary`
```
DESCRIPTION:
Show a summarized hardware report for all Laptops

USAGE:
    rpk Laptops summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops tree`
```
DESCRIPTION:
Display the dependency tree for a Laptop

USAGE:
    rpk Laptops tree <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops cpu`
```
DESCRIPTION:
Manage CPUs attached to Laptops

USAGE:
    rpk Laptops cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <Laptop>            Add a CPU to a Laptop     
    set <Laptop> <index>    Update a Laptop CPU       
    del <Laptop> <index>    Remove a CPU from a Laptop
```

## `rpk Laptops cpu add`
```
DESCRIPTION:
Add a CPU to a Laptop

USAGE:
    rpk Laptops cpu add <Laptop> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name

OPTIONS:
    -h, --help       Prints help information  
        --model      The model name           
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk Laptops cpu set`
```
DESCRIPTION:
Update a Laptop CPU

USAGE:
    rpk Laptops cpu set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name            
    <index>     The index of the Laptop cpu

OPTIONS:
    -h, --help       Prints help information  
        --model      The cpu model            
        --cores      The number of cpu cores  
        --threads    The number of cpu threads
```

## `rpk Laptops cpu del`
```
DESCRIPTION:
Remove a CPU from a Laptop

USAGE:
    rpk Laptops cpu del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop               
    <index>     The index of the Laptop cpu to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops drive`
```
DESCRIPTION:
Manage storage drives attached to Laptops

USAGE:
    rpk Laptops drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <Laptop>            Add a drive to a Laptop     
    set <Laptop> <index>    Update a Laptop drive       
    del <Laptop> <index>    Remove a drive from a Laptop
```

## `rpk Laptops drive add`
```
DESCRIPTION:
Add a drive to a Laptop

USAGE:
    rpk Laptops drive add <Laptop> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in Gb    
```

## `rpk Laptops drive set`
```
DESCRIPTION:
Update a Laptop drive

USAGE:
    rpk Laptops drive set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name          
    <index>     The drive index to update

OPTIONS:
    -h, --help    Prints help information     
        --type    The drive type e.g hdd / ssd
        --size    The drive capacity in Gb    
```

## `rpk Laptops drive del`
```
DESCRIPTION:
Remove a drive from a Laptop

USAGE:
    rpk Laptops drive del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop          
    <index>     The index of the drive to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk Laptops gpu`
```
DESCRIPTION:
Manage GPUs attached to Laptops

USAGE:
    rpk Laptops gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <Laptop>            Add a GPU to a Laptop     
    set <Laptop> <index>    Update a Laptop GPU       
    del <Laptop> <index>    Remove a GPU from a Laptop
```

## `rpk Laptops gpu add`
```
DESCRIPTION:
Add a GPU to a Laptop

USAGE:
    rpk Laptops gpu add <Laptop> [OPTIONS]

ARGUMENTS:
    <Laptop>    The name of the Laptop

OPTIONS:
    -h, --help     Prints help information     
        --model    The Gpu model               
        --vram     The amount of gpu vram in Gb
```

## `rpk Laptops gpu set`
```
DESCRIPTION:
Update a Laptop GPU

USAGE:
    rpk Laptops gpu set <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name               
    <index>     The index of the gpu to update

OPTIONS:
    -h, --help     Prints help information     
        --model    The gpu model name          
        --vram     The amount of gpu vram in Gb
```

## `rpk Laptops gpu del`
```
DESCRIPTION:
Remove a GPU from a Laptop

USAGE:
    rpk Laptops gpu del <Laptop> <index> [OPTIONS]

ARGUMENTS:
    <Laptop>    The Laptop name               
    <index>     The index of the Gpu to remove

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
    summary            Show a summary report for all services                             
    add <name>         Add a new service                                                  
    list               List all services                                                  
    get <name>         Retrieve a service by name                                         
    describe <name>    Show detailed information about a service                          
    set <name>         Update properties of a service                                     
    del <name>         Delete a service                                                   
    subnets            List subnets associated with a service, optionally filtered by CIDR
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
    -h, --help        Prints help information             
        --ip          The ip address of the service       
        --port        The port the service is running on  
        --protocol    The service protocol                
        --url         The service URL                     
        --runs-on     The system the service is running on
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

