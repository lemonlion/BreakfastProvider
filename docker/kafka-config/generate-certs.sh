#!/bin/bash

set -e

VALIDITY_IN_DAYS=365
WORKING_DIRECTORY="certificates"
SECRET="certificatePassword123"

echo "Creating working folder: ${WORKING_DIRECTORY}"
mkdir -p "${WORKING_DIRECTORY}"

echo "Creating the CA key"
openssl genpkey -algorithm RSA -out "${WORKING_DIRECTORY}/ca.key"

echo "Creating the CA certificate"
openssl req -new -x509 -key "${WORKING_DIRECTORY}/ca.key" -out "${WORKING_DIRECTORY}/ca.crt" -days $VALIDITY_IN_DAYS -subj "/CN=ca.local"

echo "Creating a keystore and generate a key pair for Kafka"
keytool -genkeypair -alias localhost -keyalg RSA -dname "CN=kafka.local" -keystore "${WORKING_DIRECTORY}/kafka.keystore.jks" -storepass $SECRET -keypass $SECRET

echo "Generating a Certificate Signing Request (CSR) from the keystore"
keytool -certreq -alias localhost -file "${WORKING_DIRECTORY}/kafka.csr" -keystore "${WORKING_DIRECTORY}/kafka.keystore.jks" -storepass $SECRET

echo "Signing the CSR with the CA certificate"
openssl x509 -req -in "${WORKING_DIRECTORY}/kafka.csr" -CA "${WORKING_DIRECTORY}/ca.crt" -CAkey "${WORKING_DIRECTORY}/ca.key" -out "${WORKING_DIRECTORY}/kafka.crt" -days $VALIDITY_IN_DAYS -CAcreateserial

echo "Deleting Certificate Signing Request"
rm "${WORKING_DIRECTORY}/kafka.csr"

echo "Deleting the CA key"
rm "${WORKING_DIRECTORY}/ca.key"

echo "Importing the CA certificate into the keystore"
keytool -import -file "${WORKING_DIRECTORY}/ca.crt" -keystore "${WORKING_DIRECTORY}/kafka.keystore.jks" -alias CARoot -storepass $SECRET -noprompt

echo "Importing the signed Kafka certificate into the keystore"
keytool -import -file "${WORKING_DIRECTORY}/kafka.crt" -keystore "${WORKING_DIRECTORY}/kafka.keystore.jks" -alias localhost -storepass $SECRET -noprompt

echo "Creating a truststore and import the CA certificate"
keytool -import -file "${WORKING_DIRECTORY}/ca.crt" -keystore "${WORKING_DIRECTORY}/kafka.truststore.jks" -alias CARoot -storepass $SECRET -noprompt

echo "Inserting root CA to local Trusted Root Certificates"
sudo cp "${WORKING_DIRECTORY}/ca.crt" /usr/local/share/ca-certificates/ca.crt
sudo update-ca-certificates