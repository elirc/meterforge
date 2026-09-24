# Stops and removes the lab container. The database lives inside the container, so this discards all lab data.
param([string]$Name = "meterforge-training-mssql")
docker rm -f $Name
