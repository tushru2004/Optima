# Optima Project

## Overview

- A C# application for managing gateway configurations and updates.
- Utilizes NATS messaging for communication.
- Handles JSON-based configuration and device data.

## Features

- **NATS Integration**: Connects to a NATS server for message publishing and subscription.
- **Configuration Management**: Requests and processes gateway configuration data.
- **JSON Handling**: Reads, writes, and formats JSON files.
- **Real-Time Updates**: Subscribes to server updates for dynamic message handling.

## Installation

- Clone the repository:
  ```bash
  git clone git@github.com:tushru2004/Optima.git
  cd Optima

## Requirements

- Server should be able to push updates:
  ```bash
  git clone git@github.com:tushru2004/Optima.git
  cd Optima 

- Gateway should be able to get a config on restart (after shutdown or crash):
   ```bash
  git clone git@github.com:tushru2004/Optima.git
  cd Optima 