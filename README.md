# fn-trading
Trading system based on fundamental analysis.

## Origin
This code belongs to my master thesis located [here](thesis/Thesis.pdf). It's a coding part of this thesis.

## What does it do?
This program grabs fundamental data from various sources. Cleans them, prepare them and then feeds neural network with them. Neural network creates predictions of stocks which are used for the trading system afterwards.

## Data sources
* Stock prices
* Weather
* Forex
* Google Trends
* WikiTrends
* Futures
* Fundamentals - population, unemployment, mortality etc.

## How to run it
Because the program leverages various data sources, by now it'll probably fail to grab this data. You can circumvent this obstacle by using already processed data which were used in the thesis. These data are stored in [1 - FullData.csv](data/TradingData/1%20-%20FullData.csv) and programs targets them by default (see [Program.cs](src/Manager/Program.cs)).

