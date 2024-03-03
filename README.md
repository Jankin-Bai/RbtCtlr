# RobotInterface.cs

这段代码主要涉及到Unity的MonoBehaviour，因此它的执行是在Unity主线程上的。以下是关于线程关系的描述：

1. **主线程（Unity主线程）：**
   - Unity的生命周期方法（如`Start()`、`FixedUpdate()`、`OnApplicationQuit()`等）以及与UI相关的操作都是在Unity主线程中执行的。
   - 所有的Unity脚本（MonoBehaviour）都在主线程中运行。

2. **子线程：**
   - 代码中并未显式创建新的线程，因此主要逻辑在Unity主线程中执行。

3. **协程（Coroutine）：**
   - 协程是一种特殊的子例程，可以在延迟一定时间后继续执行，主要用于处理异步操作。
   - 例如，`ReadOnMouseOver()` 是一个协程，它通过 `yield return new WaitForSeconds(0.15f)` 实现了每隔一段时间更新一次文本。

4. **FixedUpdate 方法：**
   - `FixedUpdate()` 方法是在固定的时间间隔内调用的，通常用于处理物理相关的操作。
   - 在这个方法中，执行了一些操作，例如清理无效的机器人控制器等。

总体而言，该代码中的主要逻辑在Unity主线程中执行，而协程则提供了一些异步处理的能力，不会创建额外的线程。涉及到的线程主要是Unity引擎自身的主线程。

这段代码主要是关于机器人控制的逻辑，以下是一些主要的部分和功能：

1. **命名空间和枚举：**
   - `control_mode` 和 `control_module` 是两个枚举类型，分别表示控制模式和控制单元。
   - `namespace TouchRobot.Logic` 包含了机器人逻辑相关的类。

2. **`RobotInterface` 类：**
   - `RobotInterface` 类继承自 `MonoBehaviour`，这使得它可以作为 Unity 游戏对象上的组件使用。
   - 定义了一些枚举和静态变量，包括机器人连接状态的枚举 `status_robot_connect`，获取当前时间的属性 `time_to_string`，已连接机器人数量的属性 `count_online_robot` 等。
   - 包含了两个键盘控制器对象 `rc_keyboard_wsad` 和 `rc_keyboard_arrow`，它们用于模拟键盘输入的机器人控制。
   - `list_online_robot` 和 `list_garbage_robot` 分别表示已连接的机器人和即将断开的机器人。
   - 包含一些静态方法用于操作和管理机器人，如 `GetRobotControllerByPortName`、`NewRobotController`、`RemoveController` 等。
   - 包含一些生命周期函数，如 `FixedUpdate` 在固定时间间隔后关闭线程，`Start` 在游戏开始时初始化。

3. **机器人控制器类 `RobotController`：**
   - `RobotController` 类是一个抽象基类，包含了串口通信的逻辑。
   - 定义了一系列内部状态和方法，如串口状态、通信线程、数据处理方法等。
   - 通过 `InterfaceCallXXX` 系列方法实现机器人的初始化、游戏开始和停止、关闭机器人等功能。
   - 有一些虚拟方法，如 `InternalWrite`、`InternalEmpty` 等，子类可以覆盖这些方法以实现具体的数据写入和处理逻辑。

4. **键盘控制器类 `KeyboardController`：**
   - `KeyboardController` 类继承自 `RobotController`，表示通过键盘模拟的机器人控制器。
   - 通过键盘输入模拟机器人的运动和操作，与真实机器人的串口通信逻辑略有不同。

5. **生命周期管理：**
   - 在 `Start` 方法中初始化了一些对象，包括两个键盘控制器，同时将它们添加到 `list_online_robot` 中。
   - 在 `FixedUpdate` 方法中定期检查并关闭即将断开的机器人。
   - 在 `OnApplicationQuit` 方法中处理应用程序退出时的清理操作，关闭所有已连接的机器人。

其中包括键盘控制和串口通信控制。通过 `MonoBehaviour` 生命周期管理机器人对象。

# RobotController.cs

这段代码主要涉及到Unity的MonoBehaviour以及C#的多线程编程。以下是关于线程关系的描述：

1. **主线程（Unity主线程）：**
   - Unity的生命周期方法（如`Start()`、`Update()`等）以及与UI相关的操作都是在Unity主线程中执行的。
   - 所有的Unity脚本（MonoBehaviour）都在主线程中运行。

2. **子线程：**
   - 代码中使用了两个子线程 `thread_read` 和 `thread_write`。
   - `thread_read` 用于读取串口数据并处理，其入口函数为 `PortRead()`。
   - `thread_write` 用于写入控制命令到串口，其入口函数为 `PortWrite()`。

3. **串口数据读取线程 (`thread_read`)：**
   - `PortRead()` 方法在一个独立的线程中执行，用于读取串口数据。
   - 在循环中，通过 `my_port.Read(received_data, 0, len)` 读取数据，并进行相应处理。
   - 该线程可能被中断或异常终止。

4. **串口数据写入线程 (`thread_write`)：**
   - `PortWrite()` 方法在一个独立的线程中执行，用于写入控制命令到串口。
   - 通过调用 `async_write()` 方法执行写入操作。
   - 该线程在循环中等待写入操作完成，并通过 `Thread.SpinWait(10)` 进行自旋等待。

5. **其他线程关系：**
   - 代码中的部分操作涉及多线程，例如 `async_write` 委托的使用，但这些线程并未显式创建，而是通过线程池等机制进行管理。

总体而言，该代码主要在Unity主线程中执行生命周期方法和部分逻辑，同时通过两个子线程处理串口数据的读写操作。需要注意在多线程编程中，要确保线程之间的同步和异常处理。

这段代码是一个触摸机器人逻辑的C#脚本，主要包含了`RobotController`类和`RobotParameters`类。以下是对主要部分的详细分析：

### `RobotController` 类

1. **成员变量：**
   - `my_module`: 用于表示机器人控制器的类型，是 `control_module` 枚举类型。
   - `my_status`: 表示机器人控制器的连接状态，是 `RobotInterface.status_robot_connect` 枚举类型。
   - `my_game_controller`: 用于控制游戏的对象，通过 `GameInterface.GenerateGameController` 方法创建。
   - `my_parameter`: 机器人参数对象，类型为 `RobotParameters`。
   - `temp_port_name`: 用于存储临时的端口名称。
   - `my_port`: SerialPort 对象，用于串口通信。
   - `thread_read` 和 `thread_write`: 分别用于读和写的线程。
   - `thread_on`: 控制线程运行的标志。
   - `tick_read` 和 `tick_write`: 用于计算读和写的速率。
   - `rate_read` 和 `rate_write`: 读和写的速率。
   - `write_count` 和 `read_count`: 分别表示写和读的计数。
   - `baud_rate`: 波特率，默认为 921600。
   - `interface_command`: 一个委托，用于执行机器人的接口命令。
   - `async_write`: 一个委托，用于异步写入数据。

2. **方法：**
   - `InterfaceCallInitRobot()`: 初始化机器人控制器，包括打开串口、启动读和写线程。
   - `InterfaceCallGameStart()`: 游戏开始时的接口，留空。
   - `InterfaceCallGameStop()`: 游戏停止时的接口，设置 `interface_command` 为空函数，重置机器人参数。
   - `InterfaceCallShutRobot(bool direct)`: 关闭机器人控制器的接口，通过参数 `direct` 控制是直接关闭还是正常断开。
   - `InterfaceCallShutRobot()`: 重载的关闭机器人控制器的接口，调用 `InterfaceCallShutRobot(true)`。
   - `InternalCallWrite()`: 内部方法，调用 `InternalWrite`。
   - `InternalWrite()`: 内部方法，用于将机器人的控制数据写入串口。
   - `InternalEmpty()`: 内部空函数，可在子类中覆盖。

3. **其他方法：**
   - `CloseThread()`: 关闭线程，包括中断和终止读和写线程，并关闭串口。
   - `OnInitCommand()`: 初始化控制命令。

4. **生命周期方法：**
   - `OnInitUSB(string str)`: 初始化串口，包括设置串口参数、打开串口，创建读线程。
   - `OnPortNormalDisconnect()`: 正常断开串口，包括关闭线程、移除游戏控制器、异常处理。
   - `OnPortAbnormalDisconnect(Exception ex, RobotInterface.status_robot_connect status)`: 异常断开串口，包括关闭线程、移除游戏控制器、异常处理。

### `RobotParameters` 类

1. **成员变量：**
   - `my_ratio`: 数据转换的比例对象，类型为 `DataConverter.converting_ratio`。
   - `udp_send_msg`: 存储UDP发送消息的字节数组。
   - `handle_sw_last_state`: 用于存储上一次 handle_sw 的状态。
   - `handle_sw_trigger`: 用于触发 handle_sw 的标志。

2. **方法：**
   - `SpeedIncKeyboard(Vector2 val, bool button, GameController gc)`: 根据键盘输入调整速度和位置，并触发游戏前馈和反馈。
   - `GameFeedback(GameController gc = null)`: 游戏反馈，将机器人输出数据转换为游戏可用数据。
   - `GameFeedforward(GameController gc = null)`: 游戏前馈，将接收到的数据传递给游戏控制器。
   - `ModifyDataToRobot`: 数据转换，将标准化数据或降采样数据转换为机器人可用数据。
   - `ModifyDataToGame`: 数据转换，将降采样数据转换为游戏可用数据。
   - `PackControlMessage()`: 打包控制消息，根据控制模式设置udp_send_msg中的相应字节。
   - `PackDesiredCurrent()`, `PackDesiredPosition()`, `PackDesiredSpeed()`: 打包期望的电流、位置和速度。
   -

# RobotController2D.cs

提供的代码主要是使用C#编写的，涉及到了一些类和命名空间，主要功能是控制一个2D机器人。以下是代码中的线程关系：

1. **命名空间 `TouchRobot.Logic`**：
    - 包含机器人控制的主要逻辑。

2. **类 `RobotController2D`**：
    - 继承自 `RobotController`。
    - 代表一个2D机器人控制器。
    - 使用了一些库和类，如 `standardlized_data`、`my_delegate`、`RobotParameter2D`等。
    - 使用多线程进行端口读写。

3. **`RobotController2D` 类中的方法**：
    - `InternalWrite()`：处理向机器人写入数据。
    - `PortRead()`：管理负责从端口读取数据的线程。
    - `PortWrite()`：覆盖了基类的 `PortWrite` 方法。
    - `DataDisplay()`：返回用于显示的数据的字符串表示。

4. **类 `RobotParameter2D`**：
    - 继承自 `RobotParameters`。
    - 包含特定于2D机器人的参数。
    - 处理从游戏到机器人的反馈和前馈。

5. **`RobotParameter2D` 类中的方法**：
    - `GameFeedback()`：提供从游戏到机器人的反馈。
    - `GameFeedforward()`：从游戏到机器人的前馈数据。
    - `PackControlMessage()`：打包用于与机器人通信的控制消息。
    - `GetRobotData()`：从机器人获取数据。

6. **类 `TouchingItems`**：
    - 代表一组触摸项。
    - 包含用于清除项的方法。

7. **类 `ScrollingDataReader`**：
    - 管理从UDP消息中读取数据。
    - 处理UDP消息的解析以提取相关信息。
    - 使用多线程进行数据读取。

8. **`ScrollingDataReader` 类中的方法**：
    - `Read()`：读取UDP消息并提取信息。
    - `DataMismatch()`：处理数据不匹配的情况。

9. **其他细节**：
    - 代码在 `PortRead` 方法中使用了 `Thread.SpinWait(2000)`，表明使用了一种线程等待或延迟的形式。
    - 针对端口读取时的I/O异常进行了异常处理。
    - 调试日志用于各种调试目的。

总体而言，该代码设计用于一个2D机器人控制系统，使用多线程处理通信和数据处理。`RobotController2D` 类似乎是中央控制器，协调不同组件之间的通信和交互。

   这段代码看起来是一个用于Unity项目的C#脚本，涉及通过触摸输入控制2D机器人。以下是主要组件和功能的详细分析：

1. **命名空间和类结构：**
   - 代码被组织到一个命名空间 (`TouchRobot.Logic`) 中，其中包含几个类。
   - 主要类是 `RobotController2D`，似乎负责控制一个2D机器人。

2. **成员变量：**
   - `rendered_data`：`standardlized_data` 类的一个实例。
   - `interface_command`：一个委托 (`my_delegate`)，在链接建立时似乎不执行任何操作；在游戏开始时负责单位换算；在应用退出时负责中断线程和关闭串口。
   - `my_parameter`：一个 `RobotParameter2D` 的实例，可能包含与机器人相关的参数。
   - 其他用于处理端口通信和状态的成员变量。

3. **构造函数：**
   - 构造函数初始化一些变量，并为机器人控制器设置初始状态。

4. **接口方法：**
   - `InterfaceCallGameStart`、`InterfaceCallGameStop` 等方法似乎提供了一个接口，允许外部系统与机器人控制器交互。

5. **InternalWrite 方法：**
   - 该方法似乎处理通过串口（RS485）向机器人写入数据。
   - 它包括数据转换和写入反馈消息。

6. **PortRead 和 PortWrite 方法：**
   - `PortRead` 似乎是一个线程，不断地从串口读取数据并进行处理。
   - `PortWrite` 没有显示，但可能用于向串口写入数据。

7. **RobotParameter2D 类：**
   - 该类似乎包含与2D机器人相关的参数，例如触摸数据和控制消息。
   - 它包含在游戏控制器和机器人之间进行数据转换的方法。

8. **TouchingItems 和 ScrollingDataReader 类：**
   - `TouchingItems` 似乎表示一组触摸项，其中每个项都是 `RobotParameter2D.touching_item` 的实例。
   - `ScrollingDataReader` 负责从UDP消息中读取和解析数据，提取与触摸相关的信息。

# DataRecorder.cs

这段代码是一个用于Unity项目的C#脚本，主要功能是实现数据记录的逻辑。以下是对主要部分的分析：

1. **宏定义部分：**
   - 使用了一些预处理宏定义，例如 `#define stop_watch` 和 `#define display_message`，用于条件编译。还有一些与消息显示相关的宏，例如 `#if display_fail`，用于控制是否显示特定类型的消息。

2. **命名空间和类：**
   - 代码被组织在 `TouchRobot.UI` 命名空间下，包含了一个名为 `DataRecorder` 的类，该类继承自 `MonoBehaviour`。

3. **成员变量和静态变量：**
   - 包含了一系列成员变量和静态变量，用于存储按钮、颜色、文件路径、线程等相关信息。
   - 使用 `Color` 类型存储按钮的按下和未按下状态的颜色。
   - 使用 `StringBuilder` 存储文本数据。
   - 使用 `Thread` 处理数据记录和定时器。

4. **委托和事件处理：**
   - 定义了一个委托 `my_delegate` 以及一个事件处理委托 `re_record_event`，用于执行机器人的接口命令。
   - 提供了一个内部方法 `GameInterfaceCallReRecord`，在事件发生时调用该方法，委托 `re_record_event` 会执行相关的处理。

5. **数据记录方法和线程管理：**
   - 提供了 `DataRecording` 方法，用于在一个单独的线程中执行数据记录的逻辑。
   - 使用 `Thread` 类初始化和管理数据记录线程，通过 `Start` 和 `Abort` 控制线程的启动和中断。
   - 提供了 `InitiateThreadDataRecording` 方法，用于初始化数据记录线程，根据线程操作类型启动、停止或重新记录。

6. **按钮操作和颜色变化：**
   - 提供了按钮操作方法 `PressDataRecording` 以及对应的静态方法 `_PressDataRecording`，用于启动或停止数据记录，并修改按钮的颜色。
   - 在 `Update` 方法中检查数据记录的时间是否超过设定的最大长度，并在超过时停止数据记录。

7. **定时器和生命周期方法：**
   - 提供了定时器方法 `GetTime` 用于获取当前时间，定时执行。
   - 在 `Start` 方法中初始化数据记录线程和定时器线程。
   - 在 `OnApplicationQuit` 方法中处理应用程序退出时的清理操作，停止数据记录和定时器。

8. **消息管理：**
   - 包含一个名为 `MessageManager` 的类，用于控制消息的显示和输出。
   - 定义了消息的类型枚举 `info_type`，以及一些显示图标的方法。
   - 提供了 `to_string` 方法，根据消息类型和宏定义进行条件显示，输出相应的消息。

实现了数据记录的逻辑，通过线程管理和委托机制，以及消息管理，实现了按钮操作、颜色变化、定时器和数据记录的功能。消息的显示受到预处理宏定义的控制，可以灵活地选择显示或隐藏不同类型的消息。

# RobotInterfaceUIManager.cs

这段代码是一个用于Unity项目的C#脚本，实现了与机器人界面相关的UI管理功能。以下是对主要部分的详细解释：

1. **成员变量和静态变量：**
   - 包含了一系列成员变量，如按钮、颜色、文本等UI元素，以及一些静态变量用于管理UI元素的颜色等信息。
   - 使用 `Color` 类型存储按钮的不同状态下的颜色。
   - 使用静态列表 `list_ui_subelement` 存储机器人界面的子元素。

2. **协程和文本更新：**
   - 使用协程 `ReadOnMouseOver` 实现鼠标悬停时更新文本的功能，通过 `yield return new WaitForSeconds(0.15f)` 实现每隔一段时间更新一次。
   - 提供了 `ReadText` 方法，用于更新悬停时显示的文本内容。

3. **UI元素的操作和管理：**
   - 提供了 `OperateOnConnect` 方法，用于根据机器人连接状态操作机器人的UI元素。
   - 提供了 `RemoveAllUI` 方法，用于移除所有UI元素。
   - 提供了 `ListSerialPort` 和 `InitializeArray` 方法，用于获取可用的串口列表和初始化UI元素。
   - 提供了 `InitializeElement` 方法，用于初始化每个串口对应的UI元素。

4. **机器人连接状态的处理：**
   - 提供了 `FindRobotInterfaceByPort` 方法，用于通过串口名查找对应的UI元素。
   - 在连接状态变化时，通过 `OperateOnConnect` 方法更新UI元素的状态。

5. **UI元素的显示和隐藏：**
   - 提供了 `FreshUI`、`FocusUI` 和 `HideUI` 方法，分别用于刷新UI、聚焦UI和隐藏UI。
   - 在 `HideUI` 方法中通过修改 `RectTransform` 的 `anchoredPosition` 实现UI的显示和隐藏。

6. **模块选择和界面初始化：**
   - 提供了 `SelectModule` 方法，用于选择控制模块。
   - 在 `Start` 方法中通过 `FreshUI` 初始化UI元素，并启动协程更新悬停文本。

7. **界面布局和样式设置：**
   - 使用一些静态变量定义了UI元素的布局和样式，如按钮的宽度、高度，间隔等。
   - 在 `Start` 方法中获取并存储了一些颜色信息，同时初始化UI。

8. **生命周期方法：**
   - 在 `OnApplicationQuit` 方法中停止所有协程。

总体而言，这段代码实现了机器人UI界面的管理，包括串口的获取、UI元素的创建、连接状态的更新、UI的显示和隐藏等功能。通过协程实现了悬停时文本的动态更新，增强了用户体验。
