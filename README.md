# This guide is for Raspberry Pi 2, ARM32

## Install

`sudo apt install git`

`wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh`

`chmod +x ./dotnet-install.sh`

`./dotnet-install.sh --version latest`

`dotnet --info`

# Build ARM

`useradd --system --no-create-home wol`

`sudo mkdir -p /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo dotnet publish --runtime linux-arm --self-contained -o /opt/wol`

# Build x64

`useradd --system --no-create-home wol`

`sudo mkdir -p /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo dotnet publish --runtime linux-x64 --self-contained -o /opt/wol`

# Register service

`sudo cp wol.service /etc/systemd/system/wol.service`

`sudo chown root:root /etc/systemd/system/wol.service`

`sudo systemctl enable wol.service`

`sudo systemctl start wol.service`

`sudo systemctl status wol.service`

# Update ARM

`git pull`

`sudo systemctl stop wol.service`

`sudo dotnet publish --runtime linux-arm --self-contained -o /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo systemctl restart wol.service`

`sudo systemctl status wol.service`

# Update x64

`git pull`

`sudo systemctl stop wol.service`

`sudo dotnet publish --runtime linux-x64 --self-contained -o /opt/wol`

`sudo chown -R wol:wol /opt/wol/`

`sudo systemctl restart wol.service`

`sudo systemctl status wol.service`