using Chow.Interpreter;
var module = new ChowModule();

var code =
    """
    counter = 0 

    # start is called once before the first execution of update after the behavior is created
    def start():
    	global game_object
    	print("start called")

    # update is called once per frame
    def update():
    	global game_object, counter
    	counter += 1
    	if counter == 120:
    		print("120 frames hit!")
    		counter = 0
    """;

module.Execute(code);
