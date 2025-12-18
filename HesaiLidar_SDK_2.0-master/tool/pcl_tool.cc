#include <cstdint>
#define NOMINMAX
#include "hesai_lidar_sdk.hpp"
#define PCL_NO_PRECOMPILE
#include <pcl/point_types.h>
#include <pcl/visualization/pcl_visualizer.h>
#include <pcl/io/pcd_io.h>
#include <pcl/io/ply_io.h>

using namespace hesai::lidar;
#include <thread>
#include <csignal>
#include <mutex>

bool stop_signal = false;

void SignalHandler(int signum)
{
    stop_signal = true;
    printf("recevice exit signal\n");
}



/* ------------Select the fields to be exported ------------ */
#define ENABLE_TIMESTAMP
#define ENABLE_RING
#define ENABLE_INTENSITY
// #define ENABLE_CONFIDENCE
// #define ENABLE_WEIGHT_FACTOR
// #define ENABLE_ENV_LIGHT

/* ------------Select the required file format ------------ */
// #define SAVE_PCD_FILE_ASCII
// #define SAVE_PCD_FILE_BIN
// #define SAVE_PCD_FILE_BIN_COMPRESSED
// #define SAVE_PLY_FILE
#define ENABLE_VIEWER

/* -------------------Select the test mode ------------------- */
//#define LIDAR_PARSER_TEST
// #define SERIAL_PARSER_TEST
// #define PCAP_PARSER_TEST
// #define EXTERNAL_INPUT_PARSER_TEST
#define LIDAR_PARSER_TCP_TEST


#ifdef ENABLE_TIMESTAMP
  #define TIMESTAMP_PCL_STR  (double, timestamp, timestamp)
#else
  #define TIMESTAMP_PCL_STR
#endif
#ifdef ENABLE_RING
  #define RING_PCL_STR  (std::uint16_t, ring, ring)
#else
  #define RING_PCL_STR
#endif
#ifdef ENABLE_INTENSITY
  #define INTENSITY_PCL_STR  (float, intensity, intensity)
#else
  #define INTENSITY_PCL_STR
#endif
#ifdef ENABLE_CONFIDENCE
  #define CONFIDENCE_PCL_STR  (std::uint8_t, confidence, confidence)
#else
  #define CONFIDENCE_PCL_STR
#endif
#ifdef ENABLE_WEIGHT_FACTOR
  #define WEIGHT_FACTOR_PCL_STR  (std::uint8_t, weightFactor, weightFactor)
#else
  #define WEIGHT_FACTOR_PCL_STR
#endif
#ifdef ENABLE_ENV_LIGHT
  #define ENV_LIGHT_PCL_STR  (std::uint8_t, envLight, envLight)
#else
  #define ENV_LIGHT_PCL_STR
#endif
struct PointXYZIT {
  union {
    float data[4];
    struct {
      float x;
      float y;
      float z;
    };
  };
  double timestamp;
  uint16_t ring;                   
  float intensity;
  inline PointXYZIT() : x(0), y(0), z(0), timestamp(0), ring(0), intensity(0) {}
  EIGEN_MAKE_ALIGNED_OPERATOR_NEW
};

POINT_CLOUD_REGISTER_POINT_STRUCT(PointXYZIT,
    (float, x, x)
    (float, y, y)
    (float, z, z)
    (double, timestamp, timestamp)
    (std::uint16_t, ring, ring)
    (float, intensity, intensity)
)

using namespace pcl::visualization;
std::shared_ptr<PCLVisualizer> pcl_viewer;
std::mutex mex_viewer;
uint32_t last_frame_time;
uint32_t cur_frame_time;
//log info, display frame message
void lidarCallback(const LidarDecodedFrame<PointXYZIT>  &frame) {  
  cur_frame_time = GetMicroTickCount();
  if (cur_frame_time - last_frame_time > kMaxTimeInterval) {
    printf("Time between last frame and cur frame is: %u us\n", (cur_frame_time - last_frame_time));
  }
  last_frame_time = cur_frame_time;
  printf("frame:%d points:%u packet:%u start time:%lf end time:%lf\n",frame.frame_index, frame.points_num, frame.packet_num, frame.frame_start_timestamp, frame.frame_end_timestamp);  
  pcl::PointCloud<PointXYZIT>::Ptr pcl_pointcloud(new pcl::PointCloud<PointXYZIT>);
  mex_viewer.lock();
  if (frame.points_num == 0) return;
  pcl_pointcloud->clear();
  pcl_pointcloud->resize(frame.points_num);
  pcl_pointcloud->points.assign(frame.points, frame.points + frame.points_num);
  pcl_pointcloud->height = 1;
  pcl_pointcloud->width = frame.points_num;
  pcl_pointcloud->is_dense = false;

  std::string file_name1 = "./PointCloudFrame" + std::to_string(frame.frame_index) + "_" + std::to_string(frame.frame_start_timestamp)+ ".pcd";
  std::string file_name2 = "./PointCloudFrame" + std::to_string(frame.frame_index) + "_" + std::to_string(frame.frame_start_timestamp)+ ".bin";
  std::string file_name3 = "./PointCloudFrame" + std::to_string(frame.frame_index) + "_" + std::to_string(frame.frame_start_timestamp)+ ".ply";
  std::string file_name4 = "./PointCloudFrame" + std::to_string(frame.frame_index) + "_" + std::to_string(frame.frame_start_timestamp)+ "_compress" + ".bin";
//save point cloud with pcd file(ASCII) if define SAVE_PCD_FILE_ASCII.
#ifdef SAVE_PCD_FILE_ASCII
  pcl::PCDWriter writer;
  // you can change the value of precision to adjust the precison
  int precision = 16;
  writer.writeASCII(file_name1, *pcl_pointcloud, precision);
#endif
//save point cloud with pcd file(BIN) if define SAVE_PCD_FILE_BIN.
#ifdef SAVE_PCD_FILE_BIN
  pcl::io::savePCDFileBinary(file_name2, *pcl_pointcloud);
#endif
// save point cloud with pcd file(binary_compress) if define SAVE_PCD_FILE_BIN_COMPRESSED
#ifdef SAVE_PCD_FILE_BIN_COMPRESSED
  pcl::io::savePCDFileBinaryCompressed(file_name4, *pcl_pointcloud);
#endif
//save point cloud with ply file if define SAVE_PLY_FILE
#ifdef SAVE_PLY_FILE
  pcl::PLYWriter writer1;
  writer1.write(file_name3, *pcl_pointcloud, true);
#endif    

//display point cloud with pcl if define ENABLE_VIEWER
#ifdef ENABLE_VIEWER   
  PointCloudColorHandlerGenericField<PointXYZIT> point_color_handle(pcl_pointcloud, "intensity");
  pcl_viewer->updatePointCloud<PointXYZIT>(pcl_pointcloud, point_color_handle, "pandar");
#endif
mex_viewer.unlock();
}

//display point cloud with pcl if define ENABLE_VIEWER
void PclViewerInit(std::shared_ptr<PCLVisualizer>& pcl_viewer) {
  pcl_viewer = std::make_shared<PCLVisualizer>("HesaiPointCloudViewer");
  pcl_viewer->setBackgroundColor(0.0, 0.0, 0.0);
  pcl_viewer->addCoordinateSystem(1.0);
  pcl::PointCloud<PointXYZIT>::Ptr pcl_pointcloud(new pcl::PointCloud<PointXYZIT>);
  pcl_viewer->addPointCloud<PointXYZIT>(pcl_pointcloud, "pandar");
  pcl_viewer->setPointCloudRenderingProperties(PCL_VISUALIZER_POINT_SIZE, 2, "pandar");
  return;
}

int main(int argc, char *argv[])
{
#ifdef ENABLE_VIEWER   
  PclViewerInit(pcl_viewer);
#endif 
  HesaiLidarSdk<PointXYZIT> sample;
  DriverParam param;
  
  // Parse command-line arguments for PCAP mode
  std::string pcap_path = "";
  std::string correction_path = "";
  std::string firetimes_path = "";
  
#ifdef PCAP_PARSER_TEST
  if (argc < 3) {
    printf("Usage: %s <pcap_file> <correction_file> [firetimes_file]\n", argv[0]);
    printf("Example: %s data.pcap correction.csv firetimes.csv\n", argv[0]);
    return -1;
  }
  pcap_path = argv[1];
  correction_path = argv[2];
  if (argc >= 4) {
    firetimes_path = argv[3];
  }
  printf("PCAP file: %s\n", pcap_path.c_str());
  printf("Correction file: %s\n", correction_path.c_str());
  if (!firetimes_path.empty()) {
    printf("Firetimes file: %s\n", firetimes_path.c_str());
  }
#endif

  // assign param
  param.use_gpu = true; // Changed from (argc > 1) since we're using args for files now
  
#ifdef LIDAR_PARSER_TCP_TEST
  param.input_param.source_type = DATA_FROM_LIDAR;
  // param.input_param.device_ip_address = "192.168.1.201";  // lidar ip
  param.input_param.device_ip_address = "10.5.55.14";  // lidar ip
  param.input_param.ptc_port = 9347; // lidar ptc port
  //param.input_param.udp_port = 2368; // point cloud destination port
  param.input_param.udp_port = 2311; // point cloud destination port
  param.input_param.multicast_ip_address = "239.192.20.10";

  param.input_param.use_ptc_connected = false;  // true: use PTC connected, false: recv correction from local file
  param.input_param.correction_file_path = "C:\\DEV\\HesaiLidar_SDK_2.0-master\\correction\\angle_correction\\Pandar128E3X_Angle Correction File.csv";
  param.input_param.firetimes_path = "C:\\DEV\\HesaiLidar_SDK_2.0-master\\correction\\firetime_correction\\Pandar128E3X_Firetime Correction File.csv";

  // param.input_param.use_someip = false;  // someip subscribe point cloud and fault message - not supported in this SDK version
  param.input_param.host_ip_address = "10.5.55.201"; // point cloud destination ip, local ip
  // param.input_param.fault_message_port = 9348; // fault message destination port - not supported in this SDK version
#endif

#ifdef SERIAL_PARSER_TEST
  param.input_param.source_type = DATA_FROM_SERIAL;
  param.input_param.rs485_com = "Your serial port name for receiving point cloud";
  param.input_param.rs232_com = "Your serial port name for sending cmd";
  param.input_param.point_cloud_baudrate = 3125000;
  param.input_param.correction_file_path = "Your correction file path";
#endif

#ifdef PCAP_PARSER_TEST
  param.input_param.source_type = DATA_FROM_PCAP;
  param.input_param.pcap_path = pcap_path;
  param.input_param.correction_file_path = correction_path;
  param.input_param.firetimes_path = firetimes_path;

  param.decoder_param.pcap_play_synchronization = true;
  param.decoder_param.pcap_play_in_loop = false; // pcap palyback
#endif

#ifdef EXTERNAL_INPUT_PARSER_TEST
  param.input_param.source_type = DATA_FROM_EXTERNAL_INPUT;
  param.input_param.correction_file_path = "Your correction file path";
  param.input_param.firetimes_path = "Your firetime file path";
#endif

  param.decoder_param.enable_packet_loss_tool = false;
  param.decoder_param.socket_buffer_size = 262144000;

  //init lidar with param
  sample.Init(param);

  //assign callback fuction
  sample.RegRecvCallback(lidarCallback);

  //star process thread
  last_frame_time = GetMicroTickCount();
  sample.Start();
  if (sample.lidar_ptr_->GetInitFinish(FailInit)) {
    sample.Stop();
    return -1;
  }

  while (1)
  {
#ifdef ENABLE_VIEWER   
    mex_viewer.lock();
    if(pcl_viewer->wasStopped()) break;
    pcl_viewer->spinOnce();
    mex_viewer.unlock();
#endif     
    std::this_thread::sleep_for(std::chrono::milliseconds(40));
  }
  return 0;
}
