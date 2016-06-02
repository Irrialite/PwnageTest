#!/bin/bash
mkdir -p /app/log
	
sudo rsync --delete-before --verbose --archive --exclude ".*" /app/clientTemp/ /app/client/ > /app/log/deploy.log