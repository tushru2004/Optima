# Optima Project

### Resources used

- Github Copilot
- StackOverflow
- JetBrains Rider for C#

## Installation

- Clone the repository:
  ```bash
  git clone git@github.com:tushru2004/Optima.git
  cd Optima
  ```

## Requirements

### 1. Gateway should be able to get a config on restart (after shutdown or crash):

1. Run Nats in terminal tab
   ```bash
   docker compose up nats --build
   ```

2. Run Server
   ```bash
   docker compose up server --build
   ```

3. Delete contents of config for gateway_1
   ```bash
   echo "" > Gateway/config_gateway_1.json
   ```
   The config file should be empty

4. Run Gateway 1
   ```bash
   docker compose up gateway_1 --build
   ```

5. Step 3 should fetch config from the server and save it to the disk
   ```bash  
   cat Gateway/config_gateway_1.json
   ```
   The config file should be populated with the config from the server

### 2. Server should be able to push updates:

1. Run Nats in terminal tab
   ```bash
   docker compose up nats --build
   ```

2. Run Server in terminal tab
   ```bash
   docker compose up server --build
   ```

3. Run Gateway 1 in another terminal tab
   ```bash
   docker compose up gateway_1 --build
   ```

4. Run Gateway 2 in another terminal tab
   ```bash
   docker compose up gateway_2 --build
   ```

5. Run Gateway 3 in another terminal tab
   ```bash
   docker compose up gateway_3 --build
   ```

6. Open Server/ConfigurationManagement/AllGatewayConfig.json in a text editor
   and make a change. For example. change the ip address of any tcp ip device for gateway_1.
   ***Save the file***

7. Open the config file for gateway_1
   ```bash
   cat Gateway/config_gateway_1.json
   ```
   The config file should be populated with the new ip address
   
## Improvements and Limitations

- Only push updates to gateways where the files have changed. Currently the server pushes updates to all gateways. 
  This could be changed to only push updates to the gateways whose config files have changed.
- Add subject names to configuration files in the server
- Currently, all config files are mapped one-to-one to the gateway. This is due to lack of DB implementation for config
  management on gateway
- Only 3 gateways are hardcoded for manual testing
- Assumes that the server is always up and running. No fallback mechanism for gateway to fetch config from other servers