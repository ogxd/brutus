import os, subprocess, yaml, time, asyncio, sys, requests, urllib
from discord.ext import commands
from itertools import cycle

channel = {}

async def alert(website):
  global channel
  print(f"ALERT!!! -> {website['url']}")
  await channel.send(f"ALERT!!! -> {website['url']}")

proxies = ['51.159.5.133:3128']
proxy_pool = cycle(proxies)
proxy = next(proxy_pool)

#proxy_support = urllib.request.ProxyHandler({'http' : proxy, 'https': proxy})
#opener = urllib.request.build_opener(proxy_support)
#urllib.request.install_opener(opener)

jobs = []

async def scrape(website):
  global jobs
  jobs.append(website)
  while (True):
    content = ""
    if 'render' in website and website['render']:
      content += subprocess.run([
        "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
        "--headless",
        "--disable-gpu",
        "--dump-dom",
        website['url']], capture_output=True, text=True).stdout
    else:
      resource = urllib.request.urlopen(website['url'])
      content = resource.read().decode(resource.headers.get_content_charset())

    if 'excludes' in website and website['excludes'] not in content:
      await alert(website)
      jobs.remove(website)
      return

    if 'includes' in website and website['includes'] in content:
      await alert(website)
      jobs.remove(website)
      return

    await asyncio.sleep(5 if 'delay' not in website else website['delay'])

async def main():
  config = {}

  #await client.wait_until_ready()
  global channel
  channel = client.get_channel(id=913921054177624188)

  print(f"Discord channel: {channel}")

  with open("config.yaml") as file:
    config = yaml.load(file, Loader=yaml.FullLoader)

  tasks = []

  for website in config['websites']:
    tasks.append(scrape(website))

  for task in tasks:
    await task

#---
bot = commands.Bot(command_prefix='$')

@bot.command()
async def status(ctx):

@client.event
async def on_ready():
  print('We have logged in as {0.user}'.format(client))
  await main()

client.run('OTEzOTEzODkyNjkyOTcxNTcw.YaFaow._yESra6Hd2PwxfOH75Xw8RQknKo')