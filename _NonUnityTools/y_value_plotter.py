#############################################################
## Plots values from a file with. Supports realtime value
## adding into the file. Does't support changing the already
## drawn values.
#############################################################
##File format required (replace the [ ] per values):
##[y title]
##0 [value]
##1 [value]
##...
#############################################################
## Distributed under GNU General Public License v3.0.
## Atributtion to other authors of code in their respective
## comments if needed.
#############################################################
## Author: Diego Villabrille Seca
## Email: dvs.navia@gmail.com
## Status: "Development"
#############################################################
import numpy as np
from pathlib import Path
from matplotlib import pyplot as plt
import time, threading, _thread

filePath = Path("C:/Users/dvsna/Desktop/GitHubProjects/SmartBoats/Assets/Prefabs/SavedGeneration/scores.txt")
plotName=""
plotX="Generations"
notBelowZero=True
plotAverage = 20 #number of values to average, 0 makes the average not to be drawn
plotDataInstantly=True

lineColor='red'
lineAverageColor='blue'
plt.style.use('ggplot')


def closeHandler(evt):
    global windowClosed
    windowClosed=True

#The method below is for updating both x and y values. Method originaly modified from live_plotter_xy on pylive(https://github.com/makerportal/pylive)
def live_plotter_xy(x_vec,y1_data,line1,color,scale=True,marker='o',zorder=0,lw=1,ms=1,pause_time=0.01):
    global windowClosed
    global fig
    if (line1==[]):
        if (fig == []):
            plt.ion()
            fig = plt.figure(figsize=(13,6))
            fig.canvas.mpl_connect('close_event', closeHandler)
            plt.xlabel(plotX)
            plt.ylabel(plotY)
            plt.title(plotName)
            plt.ylim(bottom =0)
            plt.show()
        line1 = plt.plot(x_vec,y1_data,color=color,marker=marker,lw=lw,ms=ms)[0]

    line1.set_data(x_vec,y1_data)

    #if the user closed the plot window exit the program
    if(not windowClosed):
        if (scale):
            plt.xlim(np.min(x_vec),np.max(x_vec))
            if np.min(y1_data)<=line1.axes.get_ylim()[0] or np.max(y1_data)>=line1.axes.get_ylim()[1]:
                if(notBelowZero):
                    plt.ylim(top =np.max(y1_data)+np.std(y1_data))
                else:
                    plt.ylim([np.min(y1_data)-np.std(y1_data),np.max(y1_data)+np.std(y1_data)])
    else:
        _thread.interrupt_main()
    
    if (pause_time>0):
        plt.pause(pause_time)

    return line1

#gets the first two values to create the plot
def tryGetFirstValues():
    global plotY
    with open(filePath, 'r') as f:
        plotY = f.readline()
        for line in f.readlines():
            index, value = line.strip().split(' ', 1)
            values.insert(int(index), int(value))

#check for new or undrawn values and draw
def mainLoop():
    global lastValueIndex
    global x_vec
    global y_vec
    global line1
    global paused
    global paused
    global currentAverageSum
    global currentAverageCount
    global xAverage
    global yAverage
    global lineAverage
    global fig

    while True:
        if(not paused):
            with open(filePath,'r') as f:
                f.readline()
                newLines=f.readlines()
                #add a new value if new lines are availible
                if (len(newLines)>len(values)):
                    line = newLines[len(values)]
                    index, value = line.strip().split(' ', 1)
                    values.insert(int(index), int(value))
                #draw a new value if we have any left
                while(len(values)>lastValueIndex):
                    x_vec = np.append(x_vec,lastValueIndex)
                    y_vec = np.append(y_vec,values[lastValueIndex])

                    #plot average line
                    if (plotAverage>1):
                        currentAverageSum+=values[lastValueIndex]
                        currentAverageCount+=1
                        if (plotAverage==currentAverageCount):
                            xAverage = np.append(xAverage, len(xAverage)*plotAverage + plotAverage/2)
                            yAverage = np.append(yAverage, float(currentAverageSum)/currentAverageCount)
                            currentAverageCount=0
                            currentAverageSum=0

                    lastValueIndex+=1

                    if(not plotDataInstantly):
                        break


            line1 = live_plotter_xy(x_vec,y_vec,line1,lineColor, True,lw=0.5,ms=1.5,pause_time=0)
            if(len(xAverage)>=2):
                lineAverage = live_plotter_xy(xAverage,yAverage,lineAverage,lineAverageColor, False, False, 1,lw=1.5,pause_time=0)
            plt.pause(0.005)

        else:
            try:
                plt.pause(0.2)
            except:
                _thread.interrupt_main()

plotY=""
values = []

tryGetFirstValues()
while (len(values)<2):
    print("Waiting to have at least 2 values...")
    time.sleep(0.2)
    tryGetFirstValues()

lastValueIndex=2
fig =[]

currentAverageSum=values[0] + values[1]
currentAverageCount=2
lineAverage = []
xAverage = np.array([])
yAverage = np.array([])

if (plotAverage==2):
    xAverage = np.append(xAverage,[0])
    yAverage = np.append(yAverage,currentAverageSum/currentAverageCount)
    currentAverageCount=0
    currentAverageSum=0

x_vec = np.array([0,1])
y_vec = np.array([values[0],values[1]])
line1 = []


paused = False
windowClosed = False

#allows for pausing the drawing process to look arround the graph
thread = threading.Thread(target=mainLoop)
thread.daemon = True
thread.start()
while True:
    if (not paused):
        pause_signal = input('Type "pause" anytime to pause the drawing process and interact whith the graph\n')
    else:
        pause_signal = input('Type anything (but "pause") to continue drawing \n')

    if (pause_signal == 'pause'):
        paused = True
    else:
        paused = False
