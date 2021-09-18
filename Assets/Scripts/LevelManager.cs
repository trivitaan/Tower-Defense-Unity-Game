using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
        //Fungsi Singleton
    private static LevelManager _instance = null;
    public static LevelManager Instance
    {
        get
        { 
            if(_instance == null)
            {
                _instance = FindObjectOfType<LevelManager>();
            }
            return _instance;
        }
    }

    [SerializeField] private Transform _towerUIParent;
    [SerializeField] private GameObject _towerUIPrefab;
    [SerializeField] private Tower[] _towerPrefabs;
    [SerializeField] private Enemy[] _enemyPrefabs;

    [SerializeField] private Transform[] _enemyPaths;
    [SerializeField] private float _spawnDelay = 5f;

    private List<Tower> _spawnedTowers = new List<Tower>();
    private List<Enemy> _spawnedEnemies = new List<Enemy>();
    private List<Bullet> _spawnedBullets = new List<Bullet>();

    private float _runningSpawnDelay = 5f;



    //Menampilkan seluruh tower yang tersedia pada UI Tower Selection 
    private void InstantiateAllTowerUI()
    {
        foreach(Tower tower in _towerPrefabs)
        {
            GameObject newTowerUIObj = Instantiate(_towerUIPrefab.gameObject, _towerUIParent);
            TowerUI newTowerUI = newTowerUIObj.GetComponent<TowerUI>();

            newTowerUI.SetTowerPrefab(tower);
            newTowerUI.transform.name = tower.name;
        }
    }

    //Mendaftarkan tower yang di-spawn agar bisa dikontrol oleh levelManager
    public void RegisterSpawnedTower(Tower tower)
    {
        _spawnedTowers.Add(tower);
    }

    private void SpawnEnemy()
    {
        int randomIndex = Random.Range(0, _enemyPrefabs.Length);
        string enemyIndexString = (randomIndex + 1).ToString();

        GameObject newEnemyObj = _spawnedEnemies.Find(e => e.gameObject.activeSelf && e.name.Contains(enemyIndexString))?.gameObject;

        if(newEnemyObj == null)
        {
            newEnemyObj = Instantiate(_enemyPrefabs[randomIndex].gameObject);
        }

        Enemy newEnemy = newEnemyObj.GetComponent<Enemy>();
        if(!_spawnedEnemies.Contains(newEnemy))
        {
            _spawnedEnemies.Add(newEnemy);
        }

        newEnemy.transform.position = _enemyPaths[0].position;
        newEnemy.SetTargetPosition(_enemyPaths[1].position);
        newEnemy.SetCurrentPathIndex(1);
        newEnemy.gameObject.SetActive(true);
    }

    //Untuk menampilkan garis penghubung dalam window scene 
    //tanpa harus di play
    private void OnDrawGizmos()
    {
        for(int i = 0; i < _enemyPaths.Length - 1; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_enemyPaths[i].position, _enemyPaths[i+1].position);
        }
    }

    public Bullet GetBulletFromPool(Bullet prefab)
    {
        GameObject newBulletObj = _spawnedBullets.Find(b => !b.gameObject.activeSelf && b.name.Contains(prefab.name))?.gameObject;

        if(newBulletObj == null)
        {
            newBulletObj = Instantiate(prefab.gameObject);
        }

        Bullet newBullet = newBulletObj.GetComponent<Bullet>();
        if(!_spawnedBullets.Contains(newBullet))
        {
            _spawnedBullets.Add(newBullet);
        }
        return newBullet;
    }

    public void ExplodeAt(Vector2 point, float radius, int damage)
    {
        foreach(Enemy enemy in _spawnedEnemies)
        {
            if(enemy.gameObject.activeSelf)
            {
                if(Vector2.Distance(enemy.transform.position, point) <= radius)
                {
                    enemy.ReduceEnemyHealth(damage);
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        InstantiateAllTowerUI();     
    }

    // Update is called once per frame
    void Update()
    {
        //Counter untuk spawn enemy dalam jeda waktu yang ditentukan
        //time.unscaledDelta time adalah delta time yang independent, tidak terpengaruh oleh apapun kecuali game object itu sendiri
        //jadi bisa digunakan sebagai penghitung waktu
        _runningSpawnDelay -= Time.unscaledDeltaTime;
        if(_runningSpawnDelay <= 0f)
        {
            SpawnEnemy();
            _runningSpawnDelay = _spawnDelay;
        }
        
        foreach(Tower tower in _spawnedTowers)
        {
            tower.CheckNearestEnemy(_spawnedEnemies);
            tower.SeekTarget();
            tower.ShootTarget();
        }

        foreach(Enemy enemy in _spawnedEnemies)
        {
            if(!enemy.gameObject.activeSelf)
            {
                continue;
            }

            //kenapa nilainya 0.1? Karena untuk lebih mentoleransi perbedaan posisi, 
            //akan terlalu sulit jika perbedaan posisinya harus 0 atau sama persis
            if(Vector2.Distance(enemy.transform.position, enemy.TargetPosition) < 0.1f)
            {
                enemy.SetCurrentPathIndex(enemy.CurrentPathIndex + 1);
                if(enemy.CurrentPathIndex < _enemyPaths.Length)
                {
                    enemy.SetTargetPosition(_enemyPaths[enemy.CurrentPathIndex].position);
                }
                else
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            else
            {
                enemy.MoveToTarget();
            }
        }
    }

}
