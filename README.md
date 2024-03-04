# This guide is for Raspberry Pi 2, ARM32

## Install

`sudo apt install git`

dotnet, download https://dotnet.microsoft.com/en-us/download/dotnet/8.0

`wget https://download.visualstudio.microsoft.com/download/pr/7ec1a911-afeb-47fa-a1d0-fa22cd980b32/157c20841cbf1811dd2a7a51bf4aaf88/dotnet-sdk-8.0.100-linux-arm.tar.gz`

`sudo rm -rf /opt/dotnet`

`sudo mkdir -p /opt/dotnet`

`sudo tar -xf dotnet-sdk-8.0.100-linux-arm.tar.gz -C /opt/dotnet`

`sudo ln -s /opt/dotnet/dotnet /usr/bin`

`dotnet --version`

# Build

`useradd --system --no-create-home wol`

`sudo mkdir -p /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo dotnet publish --runtime linux-arm --self-contained -o /opt/wol`

# Register service

`sudo cp wol.service /etc/systemd/system/wol.service`

`sudo chown root:root /etc/systemd/system/wol.service`

`sudo systemctl enable wol.service`

`sudo systemctl start wol.service`

`sudo systemctl status wol.service`

# Update

`git pull`

`sudo dotnet publish --runtime linux-arm --self-contained -o /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo systemctl restart wol.service`

`sudo systemctl status wol.service`