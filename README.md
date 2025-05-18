# Optima Project

## Resources Used

- GitHub Copilot
- StackOverflow
- JetBrains Rider for C#

---

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/tushru2004/Optima.git
   cd Optima
   ```
2. Optional But recommended (For ease of use) - Install Jetbrains Rider for C# development:
   - [JetBrains Rider](https://www.jetbrains.com/rider/)
   - Use the integrated terminal for running commands and see logs 
   - Formatting of logs are better in Rider because of the library i used for logging
3. Install Docker Desktop:
    - [Docker Desktop](https://www.docker.com/products/docker-desktop/)
---

## Requirements

### 1. **Gateway should fetch its configuration on restart (after shutdown/crash)**

1. Start NATS in a terminal:
   ```bash
   docker compose up nats --build
   ```

2. Start the server in another terminal:
   ```bash
   docker compose up server --build
   ```

3. Clear the configuration for Gateway 1:
   ```bash
   echo "" > Gateway/config_gateway_1.json
   ```
   The config file should now be empty.

4. Check the configuration in the server and note down the config for gateway with id 1:
   ```bash
   cat Server/ConfigurationManagement/AllGatewayConfigs.json
   ```

5. Start Gateway 1 in another terminal:
   ```bash
   docker compose up gateway_1 --build
   ```

6. Verify that Gateway 1 has fetched the config:
   ```bash
   cat Gateway/config_gateway_1.json
   ```
   The config file should now be populated.

---

### 2. **Server can push configuration updates**  
_(Limitation: Updates are pushed to all gateways on any config change)_

1. Reset everything:
   ```bash
   docker compose down
   ```

2. Start NATS in a terminal:
   ```bash
   docker compose up nats --build
   ```

3. Start the server in another terminal:
   ```bash
   docker compose up server --build
   ```

4. Start Gateway 1 in another terminal:
   ```bash
   docker compose up gateway_1 --build
   ```

5. Similarly start Gateway 2 and 3:
   ```bash
   docker compose up gateway_2 --build
   docker compose up gateway_3 --build
   ```

6. Modify the server-side config:
   Open `Server/ConfigurationManagement/AllGatewayConfig.json` in a text editor and edit, e.g., change a TCP/IP device's IP for Gateway 1. **Save the file** after editing.

7. Verify that the updated config is pushed to Gateway 1:
   ```bash
   cat Gateway/config_gateway_1.json
   ```

---

### 3. **NATS Server goes down**

1. Reset everything:
   ```bash
   docker compose down
   ```

2. Start NATS in a terminal:
   ```bash
   docker compose up nats --build
   ```

3. Start the server in another terminal:
   ```bash
   docker compose up server --build
   ```

4. Start Gateway 1 in another terminal:
   ```bash
   docker compose up gateway_1 --build
   ```

5. Stop the NATS server:
   ```bash
   docker compose down nats
   ```

6. Confirm retry behavior:
   After a few seconds, restart NATS:
   ```bash
   docker compose up nats --build
   ```
   Both the server and all gateways should automatically reconnect to NATS.

---
### 4. **Gateway is being restarted but cannot connect with the server**
1. Reset everything:
   ```bash
   docker compose down
   ```
2. Start NATS in a terminal:
   ```bash
   docker compose up nats --build
   ```
3. Start Gateway 1 in another terminal:
   ```bash
   docker compose up gateway_1 --build
   ```
4. Notice that the gateway will keep trying to connect to the server. After a few seconds, restart the server:
   ```bash
   docker compose up server --build
   ```
### 4. **Config File Structure Validation**

- **Gateway ID**: Must not be null or empty.
- **Gateway Name**: Must not be null or empty.
- **Facility Name**: Must not be null or empty.
- **TCP/IP Devices**: Must exist and contain at least one device.
- **Modbus RTU Devices**: Must exist and contain at least one device.

> If the configuration is invalid, the Gateway Config Validator will throw an exception on the server.

---

## Improvements & Limitations

- **Selective Updates:**  
  Push updates only to gateways whose configs have changed (Currently, updates are pushed to all gateways).

- **DB Implementation for Gateway Configuration:**  
  Replace the current one-to-one mapping with a more dynamic database-driven mechanism.

- **Hardcoded Gateways for Testing:**  
  Currently supports only 3 gateways for manual tests.

- **Reliability:**  
  No fallback mechanism for fetching config if the server is down.

- **Unit Tests:**  
  Coverage could be improved.

---