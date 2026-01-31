# CLI Commands

## `rpk`
```
USAGE:
    rpk [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary         Show a summarized report for all resources
    servers         Manage servers                            
    switches        Manage switches                           
    systems         Manage systems                            
    accesspoints    Manage access points                      
    ups             Manage UPS units                          
    desktops        Manage Desktops                           
    services        Manage services                           
```

## `rpk summary`
```
DESCRIPTION:
Show a summarized report for all resources

USAGE:
    rpk summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk servers`
```
DESCRIPTION:
Manage servers

USAGE:
    rpk servers [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show a summarized hardware report for all servers
    add <name>         Add a new server                                 
    get <name>         List servers or get a server by name             
    describe <name>    Show detailed information about a server         
    set <name>         Update server properties                         
    del <name>         Delete a server                                  
    tree <name>        Displays a dependency tree for the server        
    cpu                Manage server CPUs                               
    drive              Manage server drives                             
    gpu                Manage server GPUs                               
    nic                Manage server NICs                               
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
Add a new server

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
List servers or get a server by name

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
Show detailed information about a server

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
Update server properties

USAGE:
    rpk servers set <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help        Prints help information
        --ram <GB>                           
        --ipmi                               
```

## `rpk servers del`
```
DESCRIPTION:
Delete a server

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
Displays a dependency tree for the server

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
Manage server CPUs

USAGE:
    rpk servers cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a CPU to a server     
    set <name>    Update a CPU on a server  
    del <name>    Remove a CPU from a server
```

## `rpk servers cpu add`
```
DESCRIPTION:
Add a CPU to a server

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
Update a CPU on a server

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
Manage server drives

USAGE:
    rpk servers drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a drive to a server     
    set <name>    Update a drive on a server  
    del <name>    Remove a drive from a server
```

## `rpk servers drive add`
```
DESCRIPTION:
Add a drive to a server

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
Update a drive on a server

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
Manage server GPUs

USAGE:
    rpk servers gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a GPU to a server     
    set <name>    Update a GPU on a server  
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
Update a GPU on a server

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
Manage server NICs

USAGE:
    rpk servers nic [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>    Add a NIC to a server     
    set <name>    Update a NIC on a server  
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
Update a NIC on a server

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
Manage switches

USAGE:
    rpk switches [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show switch hardware report             
    add <name>         Add a new switch                        
    list               List switches                           
    get <name>         Get a switches by name                  
    describe <name>    Show detailed information about a switch
    set <name>         Update switch properties                
    del <name>         Delete a switch                         
```

## `rpk switches summary`
```
DESCRIPTION:
Show switch hardware report

USAGE:
    rpk switches summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches add`
```
DESCRIPTION:
Add a new switch

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
List switches

USAGE:
    rpk switches list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk switches get`
```
DESCRIPTION:
Get a switches by name

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
Update switch properties

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
Delete a switch

USAGE:
    rpk switches del <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems`
```
DESCRIPTION:
Manage systems

USAGE:
    rpk systems [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show system report                       
    add <name>         Add a new system                         
    list               List systems                             
    get <name>         Get a system by name                     
    describe <name>    Show detailed information about a system 
    set <name>         Update system properties                 
    del <name>         Delete a system                          
    tree <name>        Displays a dependency tree for the system
```

## `rpk systems summary`
```
DESCRIPTION:
Show system report

USAGE:
    rpk systems summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems add`
```
DESCRIPTION:
Add a new system

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
List systems

USAGE:
    rpk systems list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk systems get`
```
DESCRIPTION:
Get a system by name

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
Show detailed information about a system

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
Update system properties

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
Delete a system

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
Displays a dependency tree for the system

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
    summary            Show access point hardware report              
    add <name>         Add a new access point                         
    list               List access points                             
    get <name>         Get an access point by name                    
    describe <name>    Show detailed information about an access point
    set <name>         Update access point properties                 
    del <name>         Delete an access point                         
```

## `rpk accesspoints summary`
```
DESCRIPTION:
Show access point hardware report

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
List access points

USAGE:
    rpk accesspoints list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk accesspoints get`
```
DESCRIPTION:
Get an access point by name

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
Update access point properties

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
    summary            Show UPS hardware report             
    add <name>         Add a new UPS                        
    list               List UPS units                       
    get <name>         Get a UPS by name                    
    describe <name>    Show detailed information about a UPS
    set <name>         Update UPS properties                
    del <name>         Delete a UPS                         
```

## `rpk ups summary`
```
DESCRIPTION:
Show UPS hardware report

USAGE:
    rpk ups summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups add`
```
DESCRIPTION:
Add a new UPS

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
List UPS units

USAGE:
    rpk ups list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk ups get`
```
DESCRIPTION:
Get a UPS by name

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
Show detailed information about a UPS

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
Update UPS properties

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
Delete a UPS

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
Manage Desktops

USAGE:
    rpk desktops [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <name>                                     
    list                                           
    get <name>                                     
    describe <name>                                
    set <name>                                     
    del <name>                                     
    summary            Show desktop hardware report
    tree <name>                                    
    cpu                                            
    drive                                          
    gpu                                            
    nic                                            
```

## `rpk desktops add`
```
USAGE:
    rpk desktops add <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops list`
```
USAGE:
    rpk desktops list [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops get`
```
USAGE:
    rpk desktops get <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops describe`
```
USAGE:
    rpk desktops describe <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops set`
```
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
Show desktop hardware report

USAGE:
    rpk desktops summary [OPTIONS]

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops tree`
```
USAGE:
    rpk desktops tree <name> [OPTIONS]

ARGUMENTS:
    <name>     

OPTIONS:
    -h, --help    Prints help information
```

## `rpk desktops cpu`
```
USAGE:
    rpk desktops cpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>             
    set <desktop> <index>     
    del <desktop> <index>     
```

## `rpk desktops cpu add`
```
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
USAGE:
    rpk desktops drive [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>             
    set <desktop> <index>     
    del <desktop> <index>     
```

## `rpk desktops drive add`
```
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
USAGE:
    rpk desktops gpu [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>             
    set <desktop> <index>     
    del <desktop> <index>     
```

## `rpk desktops gpu add`
```
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
USAGE:
    rpk desktops nic [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    add <desktop>             
    set <desktop> <index>     
    del <desktop> <index>     
```

## `rpk desktops nic add`
```
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
USAGE:
    rpk desktops nic del <desktop> <index> [OPTIONS]

ARGUMENTS:
    <desktop>    The desktop name              
    <index>      The index of the nic to remove

OPTIONS:
    -h, --help    Prints help information
```

## `rpk services`
```
DESCRIPTION:
Manage services

USAGE:
    rpk services [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    summary            Show service summary report              
    add <name>         Add a new service                        
    list               List all services                        
    get <name>         Get a service by name                    
    describe <name>    Show detailed information about a service
    set <name>         Update service properties                
    del <name>         Delete a service                         
    subnets            List service subnets or filter by CIDR   
```

## `rpk services summary`
```
DESCRIPTION:
Show service summary report

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
Get a service by name

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
Update service properties

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
List service subnets or filter by CIDR

USAGE:
    rpk services subnets [OPTIONS]

OPTIONS:
    -h, --help               Prints help information
        --cidr <CIDR>                               
        --prefix <PREFIX>                           
```

