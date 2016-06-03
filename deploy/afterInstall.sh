#!/bin/bash
mkdir -p /app/log
	
sudo rsync --delete-before --verbose --archive --exclude ".*" --exclude "log" /app/clientTemp/ /app/client/ > /app/log/deploy.log