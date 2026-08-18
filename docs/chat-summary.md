# TacxRpmApp — Historical Chat Summary

> This is a historical development summary, not a description of the final current behavior. For the source-verified state, setup, real-device testing, and known limitations, read [current-state.md](current-state.md).

The protocol command notes below describe planned or discussed work. The current `TacxNeoService.SetResistanceAsync` intentionally does not write a command to the trainer.

Este documento resume todas as decisões, código, arquitetura e passos técnicos discutidos durante o desenvolvimento da app MAUI para controlar o Tacx Neo 2T via Bluetooth FTMS.

Objetivo do Projeto

Criar uma aplicação .NET MAUI (Android) para controlar o Tacx Neo 2T usando Bluetooth FTMS, com:

Três presets de resistência: Cruise, Uphill, Downhill

Botões de ajuste rápido (+ / –)

Incremento configurável

Interface simples para aulas RPM

Serviço BLE dedicado para enviar comandos FTMS

Estrutura da App

Projeto MAUI Single Project

Target: net8.0-android

UI em XAML

Lógica em C#

Serviço BLE usando BluetoothGatt

Comunicação FTMS via characteristic Control Point

UI (MainPage)

A interface contém:

Botão para ligar ao Tacx

Campo para definir incremento

Três presets com:

Label do valor atual

Botões + e –

Botão “Aplicar”

Valores internos dos presets:
cruise = 20
uphill = 45
downhill = 10

Incremento configurável:
Increment = valor do campo, ou 2 por defeito.

Serviço BLE (TacxNeoService)

UUIDs FTMS:

FTMS Service: 00001826-0000-1000-8000-00805f9b34fb

Control Point: 00002ad9-0000-1000-8000-00805f9b34fb

Comando usado:
Opcode 0x04 → Set Target Resistance

Envio do comando:
Enviar 3 bytes: [0x04, lowByte, highByte] para a characteristic Control Point.

Fluxo BLE:

Encontrar dispositivo Tacx emparelhado

ConnectGatt

DiscoverServices

Obter characteristic Control Point

Enviar resistência

Permissões Android

AndroidManifest.xml inclui:
BLUETOOTH
BLUETOOTH_ADMIN
BLUETOOTH_CONNECT
ACCESS_FINE_LOCATION

Git Setup (por projeto)

Configuração local:
git config user.name "Samuel"
git config user.email "email_pessoal@example.com"
git config credential.helper store

Remote HTTPS:
git remote remove origin
git remote add origin https://github.com/mu3li/TacxRpmApp.git (github.com in Bing)

Commit inicial:
git add .
git commit -m "Initial commit"

Push inicial:
git push --set-upstream origin main

Correr o Projeto (Android)

Instalar workloads (precisa admin):
dotnet workload install maui
dotnet workload install android

Build:
dotnet build

Run no Android:
dotnet build -t:Run -f net8.0-android

Notas Importantes

BLE só funciona no Android

VS Code funciona bem para MAUI (sem Hot Reload)

PAT só funciona com HTTPS

SSH exige chave pública

Depois da instalação inicial, não é preciso admin

Ideal para telemóvel Android físico (BLE real)

Próximos Passos Possíveis

Modo aula RPM (fases + timer)

Histórico de cargas

Sincronização com Spotify

Layout horizontal para tablet

GitHub Actions para gerar APK automaticamente