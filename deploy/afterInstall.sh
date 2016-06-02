#!/bin/bash
sudo rsync --delete-before --verbose --archive --exclude ".*" /app/clientTemp/ /app/client/ > /app/log/deploy.log