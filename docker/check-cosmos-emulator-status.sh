# Define the timeout in seconds
timeout_seconds=600  # Adjust as needed

# Get the start time
start_time=$(date +%s)

while true; do
    current_time=$(date +%s)
    elapsed_time=$((current_time - start_time))
    remaining_time=$((timeout_seconds - elapsed_time))

    echo "Elapsed time: $elapsed_time seconds"
    echo "Remaining time: $remaining_time seconds"

    # Check if the timeout is reached
    if [ $elapsed_time -ge $timeout_seconds ]; then
        echo "Timeout reached. Exiting..."
        break
    fi

    sleep 2

    command="curl -sf \"http://localhost:8080/ready\""

    # Print the command
    echo "$command"

    # Execute the command and store the result
    resultCommand=$(eval "$command")

    # Check the exit status of the curl command
    if [ $? -ne 0 ]; then
        echo "Curl command failed. Retrying..."
        continue
    fi

    echo "Emulator Started successfully."
	start https://localhost:8081/_explorer/index.html
	
    break

done

read -p "Finished - press any key to close" unusedvalue
