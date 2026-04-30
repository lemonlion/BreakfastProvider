#!/bin/bash

set -e

echo "Adding hosts file entry"
sudo echo "127.0.0.1 kafka.local" | sudo tee -a /etc/hosts