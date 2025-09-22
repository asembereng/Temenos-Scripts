import React from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  BarElement,
} from 'chart.js';
import { Line, Bar } from 'react-chartjs-2';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend
);

interface TrendChartsProps {
  performanceTrends: any;
  performanceBaselines: any;
}

const TrendCharts: React.FC<TrendChartsProps> = ({ performanceTrends, performanceBaselines }) => {
  // Use real data if available, otherwise generate sample data
  const getTrendData = () => {
    if (performanceTrends && performanceTrends.labels && performanceTrends.labels.length > 0) {
      return {
        labels: performanceTrends.labels,
        responseTimeData: performanceTrends.responseTimeData || [],
        throughputData: performanceTrends.throughputData || [],
        errorRateData: performanceTrends.errorRateData || [],
      };
    }

    // Fallback to sample data
    const last7Days = Array.from({ length: 7 }, (_, i) => {
      const date = new Date();
      date.setDate(date.getDate() - (6 - i));
      return date.toLocaleDateString();
    });

    const responseTimeData = Array.from({ length: 7 }, () => Math.floor(Math.random() * 500) + 200);
    const throughputData = Array.from({ length: 7 }, () => Math.floor(Math.random() * 200) + 100);
    const errorRateData = Array.from({ length: 7 }, () => Math.random() * 5);

    return { labels: last7Days, responseTimeData, throughputData, errorRateData };
  };

  const { labels, responseTimeData, throughputData, errorRateData } = getTrendData();

  // Get baseline values
  const responseTimeBaseline = performanceBaselines?.responseTimeBaseline || 300;
  const throughputBaseline = performanceBaselines?.throughputBaseline || 150;

  const responseTimeChartData = {
    labels: labels,
    datasets: [
      {
        label: 'Response Time (ms)',
        data: responseTimeData,
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        tension: 0.1,
      },
      {
        label: 'Baseline (ms)',
        data: Array(labels.length).fill(responseTimeBaseline),
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        borderDash: [5, 5],
        tension: 0.1,
      },
    ],
  };

  const throughputChartData = {
    labels: labels,
    datasets: [
      {
        label: 'Throughput (req/sec)',
        data: throughputData,
        backgroundColor: 'rgba(54, 162, 235, 0.5)',
        borderColor: 'rgba(54, 162, 235, 1)',
        borderWidth: 1,
      },
    ],
  };

  const baselineChartData = {
    labels: labels,
    datasets: [
      {
        label: 'Baseline (req/sec)',
        data: Array(labels.length).fill(throughputBaseline),
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        borderDash: [5, 5],
        tension: 0.1,
      },
    ],
  };

  const errorRateChartData = {
    labels: labels,
    datasets: [
      {
        label: 'Error Rate (%)',
        data: errorRateData,
        borderColor: 'rgb(255, 205, 86)',
        backgroundColor: 'rgba(255, 205, 86, 0.2)',
        fill: true,
        tension: 0.1,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    plugins: {
      legend: {
        position: 'top' as const,
      },
    },
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  return (
    <div className="trend-charts">
      <div className="chart-grid">
        <div className="chart-container">
          <h4>Response Time Trends</h4>
          <Line data={responseTimeChartData} options={chartOptions} />
        </div>
        
        <div className="chart-container">
          <h4>Throughput Analysis</h4>
          <Bar data={throughputChartData} options={chartOptions} />
          <div style={{ marginTop: '10px' }}>
            <Line data={baselineChartData} options={{
              ...chartOptions,
              plugins: {
                ...chartOptions.plugins,
                legend: { display: false }
              }
            }} />
          </div>
        </div>
        
        <div className="chart-container">
          <h4>Error Rate Trends</h4>
          <Line data={errorRateChartData} options={chartOptions} />
        </div>
      </div>
    </div>
  );
};

export default TrendCharts;