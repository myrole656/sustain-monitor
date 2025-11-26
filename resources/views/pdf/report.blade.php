<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Project Report</title>

    <style>
        body {
            font-family: DejaVu Sans, sans-serif;
            font-size: 14px;
            color: #333;
        }

        h1, h2, h3 {
            margin: 0;
            padding: 0;
        }

        .title {
            text-align: center;
            margin-bottom: 20px;
            font-size: 22px;
            font-weight: bold;
            text-transform: uppercase;
        }

        .section-title {
            margin-top: 25px;
            margin-bottom: 8px;
            font-size: 18px;
            font-weight: bold;
            border-bottom: 1px solid #000;
            padding-bottom: 4px;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }

        table td {
            padding: 6px;
            vertical-align: top;
        }

        .label {
            font-weight: bold;
            width: 150px;
        }

        .value {
            color: #444;
        }

        .process-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 12px;
            border: 1px solid #ddd;
        }

        .process-table th, .process-table td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: center;
        }

        .process-table th {
            background: #f2f2f2;
            font-weight: bold;
        }
    </style>
</head>
<body>

    <div class="title">Project Report</div>

    {{-- Project Info --}}
    <div class="section-title">Project Information</div>

    <table>
        <tr>
            <td class="label">Project Name:</td>
            <td class="value">{{ $project->project_name }}</td>
        </tr>
        <tr>
            <td class="label">Location:</td>
            <td class="value">{{ $project->project_location }}</td>
        </tr>
        <tr>
            <td class="label">Status:</td>
            <td class="value">{{ $project->status }}</td>
        </tr>
        <tr>
            <td class="label">Registered At:</td>
            <td class="value">{{ \Carbon\Carbon::parse($project->reg_date)->format('d M Y') }}</td>
        </tr>
    </table>

    {{-- Responder Info --}}
    <div class="section-title">Responder Information</div>

    <table>
        <tr>
            <td class="label">Responder:</td>
            <td class="value">{{ $project->user->name }}</td>
        </tr>
        <tr>
            <td class="label">Email:</td>
            <td class="value">{{ $project->user->email }}</td>
        </tr>
    </table>

    {{-- Process Info --}}
    <div class="section-title">Process Details</div>

    <table class="process-table">
        <thead>
            <tr>
                <th>Initiation</th>
                <th>Planning</th>
                <th>Monitoring</th>
                <th>Execution</th>
                <th>Closing</th>
                <th>Final Grade</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>{{ $process->initiation }}%</td>
                <td>{{ $process->planning }}%</td>
                <td>{{ $process->monitoring }}%</td>
                <td>{{ $process->execution }}%</td>
                <td>{{ $process->closing }}%</td>
                <td>{{ $process->status }}</td>
            </tr>
        </tbody>
    </table>

</body>
</html>
